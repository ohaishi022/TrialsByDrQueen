using UnityEngine;

public class Node
{
    public Vector2 position; // 노드의 월드 좌표
    public bool isWalkable; // 해당 노드가 이동 가능한지 여부
    public Node parent; // 경로 추적을 위한 부모 노드

    public int gCost; // 시작점에서 현재 노드까지의 거리
    public int hCost; // 현재 노드에서 목표 지점까지의 예상 거리

    public int fCost
    {
        get { return gCost + hCost; } // 총 비용 F = G + H
    }

    public int gridX; // 그리드 상에서의 X 좌표
    public int gridY; // 그리드 상에서의 Y 좌표

    // 생성자
    public Node(Vector2 _position, bool _isWalkable, int _gridX, int _gridY)
    {
        position = _position;
        isWalkable = _isWalkable;
        gridX = _gridX;
        gridY = _gridY;
    }
}