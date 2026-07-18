using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

//보드 씬의 로컬 카메라 연출을 담당
//턴 변경: 전체 맵 카메라 → 현재 턴 플레이어 추적 카메라
//플레이어 카메라: 우클릭 홀드 중 마우스 회전, 우클릭 해제 시 기본 각도로 복귀
//로컬 연출 전용 컴포넌트
public class BoardCameraController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameController gameController;
    [SerializeField] private PawnSpawner pawnSpawner;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera playerFollowCamera;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

    [Header("Turn Transition")]
    [Tooltip("턴 변경 시 전체 맵을 보여주는 시간")]
    [SerializeField, Min(0f)] private float overviewHoldSeconds = 0.8f;

    [Tooltip("PawnView가 생성될 때까지 기다리는 최대 시간")]
    [SerializeField, Min(0.1f)] private float pawnWaitTimeout = 5f;

    [SerializeField] private int livePriority = 20;
    [SerializeField] private int standbyPriority = 10;

    [Header("Right Mouse Orbit")]
    [SerializeField] private bool allowRightMouseOrbit = true;

    [Tooltip("UI 위에서 우클릭을 시작했을 때 카메라 회전을 막습니다.")]
    [SerializeField] private bool blockOrbitWhenPointerOverUI = true;

    [SerializeField] private bool lockCursorWhileOrbiting = true;

    [Tooltip("마우스 좌우 회전 감도")]
    [SerializeField, Min(0f)] private float horizontalSensitivity = 0.15f;

    [Tooltip("마우스 상하 회전 감도")]
    [SerializeField, Min(0f)] private float verticalSensitivity = 0.12f;

    [Header("Default Orbit")]
    [SerializeField] private float defaultYaw = 0f;
    [SerializeField] private float defaultPitch = 35f;

    [Tooltip("우클릭 해제 후 기본 각도로 돌아오는 속도")]
    [SerializeField, Min(0f)] private float returnSpeed = 120f;

    private GameSession _session;
    private Coroutine _transitionRoutine;

    private bool _playerViewActive;
    private bool _isOrbiting;

    private int _activeSlot = -1;
    private int _lastTurnNumber = -1;
    private int _lastTurnSlot = -1;

    private CursorLockMode _previousCursorLockMode;
    private bool _previousCursorVisible;

    private void Awake()
    {
        ResolveReferences();

        ResetOrbitImmediate();
        SetOverviewCameraLive();
    }

    private void Start()
    {
        if (gameController != null)
        {
            gameController.SessionReady += BindSession;

            if (gameController.Session != null)
                BindSession(gameController.Session);
        }
        else
        {
            Debug.LogError(
                "[BoardCameraController] GameController를 찾지 못했습니다.");
        }
    }

    private void OnDestroy()
    {
        if (gameController != null)
            gameController.SessionReady -= BindSession;

        UnbindSession();
        StopOrbiting();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopOrbiting();
    }

    private void ResolveReferences()
    {
        if (gameController == null)
            gameController = FindAnyObjectByType<GameController>();

        if (pawnSpawner == null)
            pawnSpawner = FindAnyObjectByType<PawnSpawner>();

        if (orbitalFollow == null && playerFollowCamera != null)
        {
            orbitalFollow =
                playerFollowCamera.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    private void BindSession(GameSession newSession)
    {
        if (_session == newSession)
        {
            RequestTurnTransition(true);
            return;
        }

        UnbindSession();

        _session = newSession;

        if (_session == null)
        {
            ShowOverviewOnly();
            return;
        }

        _session.TurnChanged += HandleTurnChanged;
        _session.PhaseChanged += HandlePhaseChanged;

        RequestTurnTransition(true);
    }

    private void UnbindSession()
    {
        if (_session == null)
            return;

        _session.TurnChanged -= HandleTurnChanged;
        _session.PhaseChanged -= HandlePhaseChanged;
        _session = null;
    }

    private void HandleTurnChanged()
    {
        RequestTurnTransition(false);
    }

    private void HandlePhaseChanged()
    {
        if (_session == null || _session.Phase != GamePhase.Board)
        {
            ShowOverviewOnly();
            return;
        }

        // 미니게임 이후 보드로 돌아온 경우,
        // 같은 슬롯/턴 번호더라도 카메라 전환을 다시 실행합니다.
        RequestTurnTransition(true);
    }

    private void RequestTurnTransition(bool force)
    {
        if (_session == null || _session.Phase != GamePhase.Board)
        {
            ShowOverviewOnly();
            return;
        }

        int currentTurn = _session.TurnNumber;
        int currentSlot = _session.CurrentTurnSlot;

        if (!force &&
            currentTurn == _lastTurnNumber &&
            currentSlot == _lastTurnSlot)
        {
            return;
        }

        _lastTurnNumber = currentTurn;
        _lastTurnSlot = currentSlot;

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine =
            StartCoroutine(TransitionToPlayerRoutine(currentSlot));
    }

    private IEnumerator TransitionToPlayerRoutine(int requestedSlot)
    {
        _playerViewActive = false;
        _activeSlot = -1;

        StopOrbiting();
        ResetOrbitImmediate();
        SetOverviewCameraLive();

        if (overviewHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(overviewHoldSeconds);

        PlayerPawnView pawn = null;
        float elapsed = 0f;

        while (elapsed < pawnWaitTimeout)
        {
            if (_session == null ||
                _session.Phase != GamePhase.Board ||
                _session.CurrentTurnSlot != requestedSlot)
            {
                _transitionRoutine = null;
                yield break;
            }

            if (pawnSpawner != null)
                pawn = pawnSpawner.GetPawnBySlot(requestedSlot);

            if (pawn != null)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (pawn == null)
        {
            Debug.LogWarning(
                $"[BoardCameraController] " +
                $"Slot {requestedSlot}의 PlayerPawnView를 찾지 못했습니다.");

            _transitionRoutine = null;
            yield break;
        }

        Transform target = pawn.CameraTarget;

        if (target == null ||
            playerFollowCamera == null)
        {
            Debug.LogWarning(
                "[BoardCameraController] 플레이어 카메라 타겟 또는 " +
                "CM_PlayerFollow 참조가 없습니다.");

            _transitionRoutine = null;
            yield break;
        }

        playerFollowCamera.Follow = target;
        playerFollowCamera.LookAt = target;

        // Standby 상태에서 새 타겟을 한 프레임 먼저 계산하도록 대기
        yield return null;

        if (_session == null ||
            _session.Phase != GamePhase.Board ||
            _session.CurrentTurnSlot != requestedSlot)
        {
            _transitionRoutine = null;
            yield break;
        }

        SetPlayerCameraLive();

        _activeSlot = requestedSlot;
        _playerViewActive = true;
        _transitionRoutine = null;

        Debug.Log(
            $"[BoardCameraController] " +
            $"현재 턴 Slot {requestedSlot} 추적 시작");
    }

    private void SetOverviewCameraLive()
    {
        if (overviewCamera != null)
            overviewCamera.Priority = livePriority;

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = standbyPriority;
    }

    private void SetPlayerCameraLive()
    {
        if (overviewCamera != null)
            overviewCamera.Priority = standbyPriority;

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = livePriority;
    }

    private void ShowOverviewOnly()
    {
        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        _playerViewActive = false;
        _activeSlot = -1;

        StopOrbiting();
        ResetOrbitImmediate();
        SetOverviewCameraLive();
    }

    private void Update()
    {
        if (orbitalFollow == null)
            return;

        if (!_playerViewActive ||
            !allowRightMouseOrbit ||
            Mouse.current == null)
        {
            if (_isOrbiting)
                StopOrbiting();

            ReturnOrbitToDefault();
            return;
        }

        if (!_isOrbiting &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            bool pointerOverUI =
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();

            if (blockOrbitWhenPointerOverUI && pointerOverUI)
                return;

            StartOrbiting();
        }

        if (_isOrbiting)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                ApplyOrbitInput(mouseDelta);
            }
            else
            {
                StopOrbiting();
            }
        }
        else
        {
            ReturnOrbitToDefault();
        }
    }

    private void StartOrbiting()
    {
        if (_isOrbiting)
            return;

        _isOrbiting = true;

        if (!lockCursorWhileOrbiting)
            return;

        _previousCursorLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void StopOrbiting()
    {
        if (!_isOrbiting)
            return;

        _isOrbiting = false;

        if (!lockCursorWhileOrbiting)
            return;

        Cursor.lockState = _previousCursorLockMode;
        Cursor.visible = _previousCursorVisible;
    }

    private void ApplyOrbitInput(Vector2 mouseDelta)
    {
        var horizontal = orbitalFollow.HorizontalAxis;
        horizontal.Value = horizontal.ClampValue(
            horizontal.Value +
            mouseDelta.x * horizontalSensitivity);
        orbitalFollow.HorizontalAxis = horizontal;

        var vertical = orbitalFollow.VerticalAxis;
        vertical.Value = vertical.ClampValue(
            vertical.Value -
            mouseDelta.y * verticalSensitivity);
        orbitalFollow.VerticalAxis = vertical;
    }

    private void ReturnOrbitToDefault()
    {
        float amount = returnSpeed * Time.unscaledDeltaTime;

        var horizontal = orbitalFollow.HorizontalAxis;
        horizontal.Value = horizontal.ClampValue(
            Mathf.MoveTowardsAngle(
                horizontal.Value,
                defaultYaw,
                amount));
        orbitalFollow.HorizontalAxis = horizontal;

        var vertical = orbitalFollow.VerticalAxis;
        vertical.Value = vertical.ClampValue(
            Mathf.MoveTowards(
                vertical.Value,
                defaultPitch,
                amount));
        orbitalFollow.VerticalAxis = vertical;
    }

    private void ResetOrbitImmediate()
    {
        if (orbitalFollow == null)
            return;

        var horizontal = orbitalFollow.HorizontalAxis;
        horizontal.Center = defaultYaw;
        horizontal.Value = horizontal.ClampValue(defaultYaw);
        orbitalFollow.HorizontalAxis = horizontal;

        var vertical = orbitalFollow.VerticalAxis;
        vertical.Center = defaultPitch;
        vertical.Value = vertical.ClampValue(defaultPitch);
        orbitalFollow.VerticalAxis = vertical;
    }
}