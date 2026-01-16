using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit_Base))]
public class Unit_Movement : MonoBehaviour
{
    private Unit_Base owner;
    private GridManager gridManager;
    private Animator animator;

    private LayerMask wallMask;
    private LayerMask wallMask_AI;
    private LayerMask blockMask;

    [Header("State")]
    [SerializeField] private bool isMoving;
    [SerializeField] private bool stopMoving;
    public bool IsMoving => isMoving;
    public bool StopMoving => stopMoving;

    [Header("Direction")]
    public Direction direction = Direction.Down;
    public Vector2 directionVector = new Vector2(0, -1);

    [Header("Positions")]
    public Vector3 startPosition;
    public Vector3 endPosition;
    public Vector3 skillPosition;

    [Header("Path")]
    public List<Node> path = new();
    public int currentPathIndex = 0;

    public enum PathSource { None, AStar, DiagonalThenStraight, AStarFallback, Error }
    [SerializeField] private PathSource currentPathSource = PathSource.None;

    [Header("AI Move Option")]
    public int stepStartPairs = 4;
    public bool primaryIsLongerAxis = true;
    public bool forcePureStraightAfterStart = true;

    [Header("Repath")]
    public int repathEveryTiles = 3;
    public int retargetInterval = 3;

    private bool repathPending;
    private int repathStepCounter;
    private int retargetTiles;

    public Func<Unit_Base> retargetResolver;

    private Coroutine moveRoutine;
    private Coroutine stopRoutine;
    private Coroutine dashRoutine;

    private static readonly int AnimX = Animator.StringToHash("X");
    private static readonly int AnimY = Animator.StringToHash("Y");
    private static readonly int AnimWalk = Animator.StringToHash("isWalk");

    // ===== Init =====
    public void Initialize(Unit_Base unit, GridManager grid, LayerMask wall, LayerMask wallAI)
    {
        owner = unit;
        gridManager = grid;
        animator = owner.animator;

        wallMask = wall;
        wallMask_AI = wallAI;
        blockMask = wallMask | wallMask_AI;

        Vector3 p = transform.position;
        p = new Vector3((int)p.x, (int)p.y, p.y * 0.01f);
        transform.position = p;

        startPosition = p;
        endPosition = p;
        skillPosition = p;
    }

    // ===== Public API =====
    public void MoveTo(Vector2 destination)
    {
        if (owner != null && owner.Status != null && !owner.Status.CanMove) return;
        if (stopMoving) return;
        if (dashRoutine != null) return; // 대쉬 중엔 일반 이동 무시

        if (!isMoving && (path == null || currentPathIndex >= path.Count))
            FindPath_DiagonalThenStraight(transform.position, destination);

        MoveAlongPath();
    }

    public void Get_Move(Vector2 inputMove)
    {
        if (owner != null && owner.Status != null && !owner.Status.CanMove) return;
        if (stopMoving || isMoving || inputMove == Vector2.zero) return;
        if (dashRoutine != null) return;

        moveRoutine = StartCoroutine(MoveRoutine(inputMove));
    }

    public void HaltMovement()
    {
        repathPending = false;
        path = null;
        currentPathIndex = 0;
    }

    public void StopMovingFor(float duration)
    {
        if (duration <= 0f) return;

        if (stopRoutine != null) StopCoroutine(stopRoutine);
        stopRoutine = StartCoroutine(StopMovingRoutine(duration));
    }

    public void SetStopMoving(bool value)
    {
        stopMoving = value;
    }

    public void Teleport(Vector2 pos)
    {
        StopMovingFor(0.1f);

        Vector3 p = new(pos.x, pos.y, pos.y * 0.01f);
        transform.position = p;
        startPosition = endPosition = skillPosition = p;

        path = null;
        currentPathIndex = 0;
    }

    // DASH
    public void Get_Dash(Vector2 dashDirection, int tiles, float dashTotalDuration, float stopMoveDuration)
    {
        if (dashDirection == Vector2.zero || tiles <= 0) return;
        if (owner != null && owner.Status != null && !owner.Status.CanMove) return;

        // 기존 이동 코루틴 중단
        CancelMoveRoutine();

        // 기존 대쉬가 있으면 중단 후 새로
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        dashRoutine = StartCoroutine(DashRoutine(dashDirection, tiles, dashTotalDuration, stopMoveDuration));
    }

    private IEnumerator DashRoutine(Vector2 dashDirection, int tiles, float dashTotalDuration, float stopMoveDuration)
    {
        // Dash 중에는 일반 이동/AI path 추종 중단
        HaltMovement();

        // stopMoving 걸기 (대쉬 후 약간 멈칫 같은 느낌)
        if (stopMoveDuration > 0f)
            StopMovingFor(stopMoveDuration);

        isMoving = true;
        if (animator != null) animator.SetBool(AnimWalk, true);
        UpdateDirection(dashDirection);

        float mult = Mathf.Max(0.01f, owner.SpeedMultiplier);

        // 배속 반영: 멀티가 커지면 더 빨리 대쉬
        float timePerTile = (dashTotalDuration / tiles) / mult;

        for (int i = 0; i < tiles; i++)
        {
            Vector3 start = skillPosition;
            Vector3 next = start + new Vector3(dashDirection.x, dashDirection.y, dashDirection.y * 0.01f);

            // 벽 체크
            RaycastHit2D hit = Physics2D.Linecast(start, next, wallMask);
            if (hit.collider != null)
            {
                transform.position = skillPosition;
                break;
            }

            float t = 0f;
            while (t < 1f)
            {
                // Freeze면 일시정지
                if (owner.SpeedMultiplier <= 0f)
                {
                    yield return null;
                    continue;
                }

                if (t >= 0.5f) skillPosition = next;

                // timePerTile 기준으로 0~1 보간
                t += Time.deltaTime / Mathf.Max(0.0001f, timePerTile);
                transform.position = Vector3.Lerp(start, next, t);
                yield return null;
            }

            transform.position = next;
            skillPosition = next;
        }

        if (animator != null) animator.SetBool(AnimWalk, false);
        isMoving = false;

        dashRoutine = null;
    }

    private void CancelMoveRoutine()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        isMoving = false;
    }

    // ===== Core Move =====
    public void MoveAlongPath_Public()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (stopMoving || isMoving || path == null || currentPathIndex >= path.Count) return;

        Node curr = gridManager.NodeFromWorldPoint(transform.position);
        Node next = path[currentPathIndex];
        if (curr == null || next == null) return;

        if (curr.gridX == next.gridX && curr.gridY == next.gridY)
        {
            // 타겟 재탐색
            retargetTiles++;
            if (retargetTiles >= retargetInterval)
            {
                retargetTiles = 0;
                if (retargetResolver != null)
                {
                    var u = retargetResolver.Invoke();
                    if (u != null && owner != null) owner.target = u.transform;
                }
            }

            // N칸마다 리패스 예약
            repathStepCounter++;
            if (repathStepCounter >= repathEveryTiles)
            {
                repathStepCounter = 0;
                repathPending = true;
            }

            currentPathIndex++;
            if (currentPathIndex >= path.Count)
            {
                path = null;
                return;
            }

            next = path[currentPathIndex];
        }

        int dx = next.gridX - curr.gridX;
        int dy = next.gridY - curr.gridY;
        Vector2 step = dx != 0 ? new Vector2(Mathf.Sign(dx), 0) : new Vector2(0, Mathf.Sign(dy));

        if (step != Vector2.zero)
            Get_Move(step);
    }

    private IEnumerator MoveRoutine(Vector2 dir)
    {
        isMoving = true;
        if (animator != null) animator.SetBool(AnimWalk, true);
        UpdateDirection(dir);

        float t = 0f;
        startPosition = transform.position;
        endPosition = startPosition + new Vector3(dir.x, dir.y, dir.y * 0.01f);

        // 벽 체크
        RaycastHit2D hit = Physics2D.Linecast(startPosition, endPosition, wallMask);
        if (hit.transform != null)
        {
            if (animator != null) animator.SetBool(AnimWalk, false);
            isMoving = false;
            yield break;
        }

        while (t < 1f)
        {
            // Freeze면 멈춤
            if (owner.SpeedMultiplier <= 0f)
            {
                yield return null;
                continue;
            }

            float speed = owner.currentSpeed * Mathf.Max(0.01f, owner.SpeedMultiplier);
            t += Time.deltaTime * speed;

            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            if (t >= 0.5f) skillPosition = endPosition;

            yield return null;
        }
        if (animator != null) animator.SetBool(AnimWalk, false);
        isMoving = false;

        // 리패스 예약이 켜져있으면, 다음 프레임에 새 경로를 계산하도록
        if (!isMoving && repathPending && owner != null && owner.target != null)
        {
            repathPending = false;
            repathStepCounter = 0;
            FindPath_DiagonalThenStraight(transform.position, owner.target.position);
        }
    }

    // Direction / Animator
    private void UpdateDirection(Vector2 dir)
    {
        if (animator != null)
        {
            animator.SetFloat(AnimX, dir.x);
            animator.SetFloat(AnimY, dir.y);
        }

        if (dir.x == 0 && dir.y == -1) direction = Direction.Down;
        else if (dir.x == -1 && dir.y == 0) direction = Direction.Left;
        else if (dir.x == 0 && dir.y == 1) direction = Direction.Up;
        else if (dir.x == 1 && dir.y == 0) direction = Direction.Right;

        directionVector = dir;
    }

    public void ChangeDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        // 축 스냅(원하면 제거 가능): 대각 입력 들어와도 4방향으로 고정
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            dir = new Vector2(Mathf.Sign(dir.x), 0f);
        else
            dir = new Vector2(0f, Mathf.Sign(dir.y));

        UpdateDirection(dir);
    }

    // ===== Pathfinding =====
    private bool TryGetNodeByCell(Vector2Int cell, out Node node)
    {
        node = null;
        if (gridManager == null || gridManager.grid == null) return false;
        if (cell.x < 0 || cell.y < 0 || cell.x >= gridManager.gridSizeX || cell.y >= gridManager.gridSizeY) return false;
        node = gridManager.grid[cell.x, cell.y];
        return node != null;
    }

    private bool IsDiagonalMove(Node currentNode, Node neighbour)
    {
        return currentNode.gridX != neighbour.gridX && currentNode.gridY != neighbour.gridY;
    }

    private bool IsWallBetween(Node currentNode, Node neighbour)
    {
        Vector2 dir = neighbour.position - currentNode.position;
        float dist = Vector2.Distance(currentNode.position, neighbour.position);
        RaycastHit2D hit = Physics2D.Raycast(currentNode.position, dir.normalized, dist, blockMask);
        return hit.collider != null;
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    private void RetracePath(Node startNode, Node endNode)
    {
        List<Node> newPath = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            newPath.Add(currentNode);
            currentNode = currentNode.parent;
        }

        newPath.Reverse();
        path = newPath;
        currentPathIndex = 0;
    }

    public void FindPath(Vector2 startPos, Vector2 targetPos)
    {
        Node startNode = gridManager.NodeFromWorldPoint(startPos);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost ||
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                    currentNode = openSet[i];
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                currentPathSource = PathSource.AStar;
                return;
            }

            foreach (Node neighbour in gridManager.GetNeighbours(currentNode))
            {
                if (IsDiagonalMove(currentNode, neighbour)) continue;
                if (!neighbour.isWalkable || closedSet.Contains(neighbour) || IsWallBetween(currentNode, neighbour)) continue;

                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }

        // 실패
        path = null;
        currentPathSource = PathSource.Error;
    }

    private bool _dtsHasActivePath;
    private Vector2Int _dtsLastTargetCell;

    public void FindPath_DiagonalThenStraight(Vector2 startPos, Vector2 targetPos)
    {
        if (gridManager == null || gridManager.grid == null)
        {
            path = null;
            currentPathSource = PathSource.Error;
            return;
        }

        Node startNode = gridManager.NodeFromWorldPoint(startPos);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPos);
        if (startNode == null || targetNode == null)
        {
            path = null;
            currentPathSource = PathSource.Error;
            return;
        }

        Vector2Int s = new Vector2Int(startNode.gridX, startNode.gridY);
        Vector2Int t = new Vector2Int(targetNode.gridX, targetNode.gridY);

        if (s == t)
        {
            path = null;
            _dtsHasActivePath = false;
            currentPathSource = PathSource.DiagonalThenStraight;
            return;
        }

        int dx = t.x - s.x;
        int dy = t.y - s.y;
        int stepX = Math.Sign(dx);
        int stepY = Math.Sign(dy);
        int absX = Mathf.Abs(dx);
        int absY = Mathf.Abs(dy);

        bool xPrimary = primaryIsLongerAxis ? (absX >= absY) : true;
        int secondaryCount = xPrimary ? absY : absX;
        int pairs = Mathf.Min(stepStartPairs, secondaryCount);

        List<Node> newPath = new List<Node>(32);
        Vector2Int p = s;

        bool StepOK(Vector2Int from, Vector2Int to)
        {
            if (!TryGetNodeByCell(from, out var a)) return false;
            if (!TryGetNodeByCell(to, out var b)) return false;
            if (!b.isWalkable) return false;

            Vector2 dir = b.position - a.position;
            float len = dir.magnitude;
            var hit = Physics2D.Raycast(a.position, dir.normalized, len, blockMask);
            if (hit.collider != null) return false;

            newPath.Add(b);
            return true;
        }

        bool TryPair(ref Vector2Int cur, bool firstIsPrimary)
        {
            Vector2Int pairStart = cur;
            Vector2Int localPrev = cur;

            Vector2Int dPrimary = xPrimary ? new Vector2Int(stepX, 0) : new Vector2Int(0, stepY);
            Vector2Int dSecondary = xPrimary ? new Vector2Int(0, stepY) : new Vector2Int(stepX, 0);

            Vector2Int first = firstIsPrimary ? dPrimary : dSecondary;
            Vector2Int second = firstIsPrimary ? dSecondary : dPrimary;

            // planned
            Vector2Int to = cur + first;
            if (StepOK(localPrev, to))
            {
                localPrev = to; cur = to;
                to = cur + second;
                if (StepOK(localPrev, to))
                {
                    cur = to;
                    return true;
                }
                else
                {
                    newPath.RemoveAt(newPath.Count - 1);
                    cur = pairStart;
                    localPrev = pairStart;
                }
            }

            // swapped
            to = cur + second;
            if (StepOK(localPrev, to))
            {
                localPrev = to; cur = to;
                to = cur + first;
                if (StepOK(localPrev, to))
                {
                    cur = to;
                    return true;
                }
            }

            cur = pairStart;
            return false;
        }

        for (int k = 0; k < pairs; k++)
        {
            if (!TryPair(ref p, true))
            {
                FindPath(startPos, targetPos);
                currentPathSource = PathSource.AStarFallback;
                _dtsHasActivePath = (path != null && path.Count > 0);
                currentPathIndex = 0;
                _dtsLastTargetCell = t;
                return;
            }
            absX--; absY--;
        }

        if (forcePureStraightAfterStart)
        {
            int remainSecondary = xPrimary ? absY : absX;
            for (int i = 0; i < remainSecondary; i++)
            {
                Vector2Int to = xPrimary ? new Vector2Int(p.x, p.y + stepY)
                                         : new Vector2Int(p.x + stepX, p.y);
                if (!StepOK(p, to))
                {
                    FindPath(startPos, targetPos);
                    currentPathSource = PathSource.AStarFallback;
                    _dtsHasActivePath = (path != null && path.Count > 0);
                    currentPathIndex = 0;
                    _dtsLastTargetCell = t;
                    return;
                }
                p = to;
            }
            if (xPrimary) absY = 0; else absX = 0;
        }

        int remainPrimary = xPrimary ? absX : absY;
        for (int i = 0; i < remainPrimary; i++)
        {
            Vector2Int to = xPrimary ? new Vector2Int(p.x + stepX, p.y)
                                     : new Vector2Int(p.x, p.y + stepY);
            if (!StepOK(p, to))
            {
                FindPath(startPos, targetPos);
                currentPathSource = PathSource.AStarFallback;
                _dtsHasActivePath = (path != null && path.Count > 0);
                currentPathIndex = 0;
                _dtsLastTargetCell = t;
                return;
            }
            p = to;
        }

        path = newPath;
        currentPathIndex = 0;
        _dtsLastTargetCell = t;
        _dtsHasActivePath = (path != null && path.Count > 0);
        currentPathSource = PathSource.DiagonalThenStraight;
    }

    // =========================================================
    // Utils
    // =========================================================

    private IEnumerator StopMovingRoutine(float duration)
    {
        stopMoving = true;
        yield return new WaitForSeconds(duration);
        stopMoving = false;
        stopRoutine = null;
    }
}