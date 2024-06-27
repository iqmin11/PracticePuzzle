using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TileInfo : MonoBehaviour
{
    public bool bIsVisit = false;
    private int TileType = 0;
    private Color TileColor = new Color();
    private Vector2Int TilePos = new Vector2Int();
    private BoardInfo ParentBoard;

    public void Setup(int x, int y, BoardInfo Parent)
    {
        SetType(Random.Range(0, 6));
        SetTilePos(new Vector2Int(x, y));
        SetParent(Parent);
    }

    private void SetParent(BoardInfo Parnet)
    {
        ParentBoard = Parnet;
    }

    public void SetTilePos(Vector2Int Pos)
    {
        TilePos = Pos;
    }

    public Vector2Int GetTilePos() 
    {
        return TilePos;
    }

    private void SetTileColor(Color Value)
    {
        GetComponent<UnityEngine.UI.Image>().color = Value;
    }

    public void SetType(int Value)
    {
        TileType = Value;

        if (TileType == 0)
        {
            TileColor = Color.red;
        }
        else if (TileType == 1)
        {
            TileColor = Color.green;
        }
        else if (TileType == 2)
        {
            TileColor = Color.blue;
        }
        else if (TileType == 3)
        {
            TileColor = Color.grey;
        }
        else if (TileType == 4)
        {
            TileColor = Color.yellow;
        }
        else if (TileType == 5)
        {
            TileColor = Color.black;
        }
        else if (TileType == -1)
        {
            TileColor = Color.white;
        }

        SetTileColor(TileColor);
    }
    
    public new int GetType()
    {
        return TileType;
    }

    public void SelectTile()
    {
        ParentBoard.PushClick(TilePos);
    }
}
