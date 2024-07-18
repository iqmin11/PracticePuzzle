using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BoardInfo : MonoBehaviour
{
    //Property//////////////////////////
    [SerializeField]
    private GameObject TilePrefab;

    [SerializeField]
    private Transform TilesParent;

    private Vector2Int PuzzleSize = new Vector2Int(8, 8);

    private List<List<TileInfo>> GridInfo = new List<List<TileInfo>>();

    private List<List<Button>> GridButton = new List<List<Button>>();

    private FSM BoardFSM;

    private List<Vector2Int> ClickPos = new List<Vector2Int>();
    private List<Vector2Int> DestroyStart = new List<Vector2Int>();
    private List<int> DestroyCount = new List<int>(8);
    
    private WaitForSeconds WaitTime;


    //Debug//////////////////////////
    void TileDebugLog(string DebugName)
    {
        DebugName += "\n";
        for (int y = 0; y < PuzzleSize.y; y++)
        {
            for (int x = 0; x < PuzzleSize.x; x++)
            {
                DebugName += GridInfo[y][x].GetType();
                DebugName += " ";
            }
            DebugName += "\n";
        }
        Debug.Log(DebugName);
    }

    //Init//////////////////////////
    private void Awake()
    {
        WaitTime = new WaitForSeconds(0.5f);
    }

    void Start()
    {
        Debug.Log("BoardInfo Start");
        GridInfo.Capacity = PuzzleSize.y;
        GridButton.Capacity = PuzzleSize.y;
        for (int y = 0; y < PuzzleSize.y; y++)
        {
            GridInfo.Add(new List<TileInfo>());
            GridInfo[y].Capacity = PuzzleSize.x;
            GridButton.Add(new List<Button>());
            GridButton[y].Capacity = PuzzleSize.x;
        }
        SpawnTiles();

        BoardFSM = GetComponent<FSM>();
        PlayStateInit();
        ConfirmStateInit();
        DestroyTileStateInit();
        SettingsStateInit();

        BoardFSM.ChangeState("ConfirmState");

        for (int i = 0; i < PuzzleSize.x; i++)
        {
            DestroyCount.Add(0);
        }

    }

    private void SpawnTiles()
    {
        for (int y = 0; y < PuzzleSize.y; y++)
        {
            for (int x = 0; x < PuzzleSize.x; x++)
            {
                GameObject Clone = Instantiate(TilePrefab, TilesParent);
                TileInfo Tile = Clone.GetComponent<TileInfo>();
                Button TileButton = Clone.GetComponent<Button>();
                GridInfo[y].Add(Tile);
                GridButton[y].Add(TileButton);
                Tile.Setup(x, y, this);
            }
        }
    }

    // Update is called once per frame
    //Use FSM : Play -> Loop(Confirm -> DestroyTile -> Setting) -> Play

    //Play//////////////////////////
    public void PushClick(Vector2Int TilePos)
    {
        ClickPos.Add(TilePos);
    }

    private void SwapTile(Vector2Int Tile1, Vector2Int Tile2)
    {
        int Tile1Type = GridInfo[Tile1.y][Tile1.x].GetType();
        GridInfo[Tile1.y][Tile1.x].SetType(GridInfo[Tile2.y][Tile2.x].GetType());
        GridInfo[Tile2.y][Tile2.x].SetType(Tile1Type);
    }

    private bool IsThreeMatch(Vector2Int TilePos)
    {
        {
            int Continueous = 0;
            int PrevType = -2;

            for (int x = -2; x <= 2; x++)
            {
                int CheckX = TilePos.x + x;
                if (CheckX < 0 || CheckX >= PuzzleSize.x)
                {
                    continue;
                }

                int CurType = GridInfo[TilePos.y][CheckX].GetType();

                if (CurType == PrevType)
                {
                    Continueous++;
                }
                else
                {
                    PrevType = CurType;
                    Continueous = 1;
                }

                if (Continueous == 3)
                {
                    DestroyStart.Add(TilePos);
                    return true;
                }
            }
        }

        {
            int Continueous = 0;
            int PrevType = -2;

            for (int y = -2; y <= 2; y++)
            {
                int CheckY = TilePos.y + y;
                if (CheckY < 0 || CheckY >= PuzzleSize.y)
                {
                    continue;
                }

                int CurType = GridInfo[CheckY][TilePos.x].GetType();

                if (CurType == PrevType)
                {
                    Continueous++;
                }
                else
                {
                    PrevType = CurType;
                    Continueous = 1;
                }

                if (Continueous == 3)
                {
                    DestroyStart.Add(TilePos);
                    return true;
                }
            }
        }

        return false;
    }

    bool AllThreeMatch()
    {
        for (int y = 0; y < PuzzleSize.y; y++)
        {
            for (int x = 0; x < PuzzleSize.x; x++)
            {
                IsThreeMatch(new Vector2Int(x, y));
            }
        }

        return DestroyStart.Count != 0;
    }

    void ButtonInterActionOff()
    {
        for (int y = 0; y < PuzzleSize.y; y++)
        {
            for (int x = 0; x < PuzzleSize.x; x++)
            {
                GridButton[y][x].interactable = false;
            }
        }
    }

    void ButtonInterActionOn()
    {
        for (int y = 0; y < PuzzleSize.y; y++)
        {
            for (int x = 0; x < PuzzleSize.x; x++)
            {
                GridButton[y][x].interactable = true;
            }
        }
    }

    //Destroy Tile//////////////////////////
    private void DestroyTile()
    {
        for(int i = 0; i < DestroyStart.Count; i++)
        {
            Vector2Int CurStart = DestroyStart[i];

            if(GridInfo[CurStart.y][CurStart.x].GetType() == -1)
            {
                continue;
            }
            
            if(GridInfo[CurStart.y][CurStart.x].bIsVisit)
            {
                continue;
            }

            DFS(CurStart.x, CurStart.y);
        }
    }

    private int[] dx = { 1, 0, -1, 0 };
    private int[] dy = { 0, 1, 0, -1 };
    private void DFS(int CurX, int CurY)
    {
        GridInfo[CurY][CurX].bIsVisit = true;

        for (int Dir = 0; Dir < 4; Dir++)
        {
            int CheckX = CurX + dx[Dir];
            int CheckY = CurY + dy[Dir];

            if(CheckX < 0 || CheckY < 0 || CheckX >= PuzzleSize.x || CheckY >= PuzzleSize.y)
            {
                continue;
            }

            if (GridInfo[CheckY][CheckX].bIsVisit)
            {
                continue;
            }

            if (GridInfo[CheckY][CheckX].GetType() != GridInfo[CurY][CurX].GetType())
            {
                continue;
            }

            DFS(CheckX, CheckY);
        }
    }

    //Setting//////////////////////////
    
    void Setting()
    {
        for (int x = 0; x < PuzzleSize.x; x++)
        {
            int VacancyCount = 0;
            for(int y = PuzzleSize.y - 1; y >= 0; y--)
            {
                if (GridInfo[y][x].bIsVisit)
                {
                    VacancyCount++;
                    GridInfo[y][x].SetType(-1);
                    GridInfo[y][x].bIsVisit = false;
                }
                else
                {
                    GridInfo[y + VacancyCount][x].SetType(GridInfo[y][x].GetType());
                }
            }

            while(VacancyCount > 0)
            {
                GridInfo[--VacancyCount][x].SetType(Random.Range(0, 6));
            }
        }
    }

    public delegate void Notify(string Param);

    IEnumerator DelayChangeState(Notify Callback, string Param, WaitForSeconds Time)
    {
        yield return Time;
        Callback(Param);
    }

    //FSM//////////////////////////
    private void PlayStateInit()
    {
        BoardFSM.CreateState("PlayState",
            () =>
            {
                Debug.Log("PlayState Start");
                //버튼 활성화
                ButtonInterActionOn();
            },

            () =>
            {
                //키 입력하면서 플레이
                if (ClickPos.Count == 2)
                {
                    SwapTile(ClickPos[0], ClickPos[1]);
                    BoardFSM.ChangeState("ConfirmState");
                    return;
                }
            },

            () =>
            {
                Debug.Log("PlayState End");
                //버튼 막기
                ButtonInterActionOff();
            }
        );
    }

    private void ConfirmStateInit()
    {
        BoardFSM.CreateState("ConfirmState",
            () =>
            {
                Debug.Log("ConfirmState Start");
            },

            () =>
            {
                ICollection<Vector2Int> SizeOfClickPos = ClickPos;
                if (SizeOfClickPos.Count == 2)
                {
                    bool CheckFirst = IsThreeMatch(ClickPos[0]);
                    bool CheckSecond = IsThreeMatch(ClickPos[1]);

                    if (CheckFirst || CheckSecond)
                    {
                        BoardFSM.ChangeState("DestroyTileState");
                        ClickPos.Clear();
                    }
                    else
                    {
                        BoardFSM.ChangeState("PlayState");
                        ClickPos.Clear();
                    }
                }
                else
                {
                    if(AllThreeMatch())
                    {
                        BoardFSM.ChangeState("DestroyTileState");
                    }
                    else
                    {
                        BoardFSM.ChangeState("PlayState");
                    }
                }
            },

            () =>
            {
                Debug.Log("ConfirmState End");
            }
        );
    }

    private void DestroyTileStateInit()
    {
        BoardFSM.CreateState("DestroyTileState",
            () =>
            {
                Debug.Log("DestroyTileState Start");
            },

            () =>
            {
                DestroyTile();
                StartCoroutine(DelayChangeState(BoardFSM.ChangeState, "SettingsState", WaitTime));
            },

            () =>
            {
                Debug.Log("DestroyTileState End");
                DestroyStart.Clear();
            }
        );
    }

    private void SettingsStateInit()
    {
        BoardFSM.CreateState("SettingsState",
            () =>
            {
                Debug.Log("SettingsState Start");
                TileDebugLog("SettingsState Start");
            },

            () =>
            {
                Debug.Log("SettingsState Update");
                Setting();
                BoardFSM.ChangeState("ConfirmState");
            },

            () =>
            {
                Debug.Log("SettingsState End");
                TileDebugLog("SettingsState End");
            }
        );
    }
}
