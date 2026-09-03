using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandPanel : MonoBehaviour
{
    [SerializeField] private GameObject player;

    public GameObject caster; // The currently selected unit or building. If nothing is selected, the player is the caster.

    public Game.Commands.CommandsData commandsData;

    [SerializeField] private GameObject prefabButton;
    private CommandButtonData[] commandButtonsData;
    private GameObject[] buttons = new GameObject[15];

    // UI scaling:
    public float UIScale = 1f;
    private RectTransform rectTransform;
    private float cellSize = 154;
    private float cellRatio = 0.071296f;
    private float spacingRatio = 0.01f;
    private float paddingRatio = 0.065f;

    // empty cell:
    [SerializeField] private Sprite emptySprite;

    private void Reset()
    {
        var controller = GetComponentInParent<IsometricPlayerController>();
        if (controller == null)
        {
            Debug.LogError("CommandPanel: Cannot find player object!");
        }
        else
        {
            player = controller.gameObject;
            caster = player;
        }
    }

    void Awake()
    {
        for (int i = 0; i < 15; i++)
        {
            buttons[i] = Instantiate(prefabButton, transform);
        }
        ResizePanel();

    }

    void DeactivatePanel()
    {
        gameObject.SetActive(false);
    }
    void ActivatePanel()
    {
        gameObject.SetActive(true);
    }

    // refresh all cells in the panel
    void UpdatePanel(CommandButtonData[] buttonsData)
    {
        commandButtonsData = buttonsData;

        if (!caster)
            caster = player;

        for (int i = 0; i < buttonsData.Length; i++)
        {
            if (buttonsData[i].isFilled)
            {
                int index = i;
                buttons[i].GetComponent<Button>().interactable = true;
                buttons[i].GetComponent<Button>().onClick.RemoveAllListeners();
                buttons[i].GetComponent<Image>().sprite = buttonsData[i].command().icon;
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = buttonsData[i].shortcut.ToString();
                if (buttonsData[i].command().callType == Game.Commands.CallType.Active || buttonsData[i].command().callType == Game.Commands.CallType.NeedAction)
                    buttons[i].GetComponent<Button>().onClick.AddListener(() => {
                        caster.GetComponent<Game.Commands.CommandCaster>()?
                        .SetCurrentCommand(buttonsData[index].command(), player.transform.position, player.transform.rotation);
                    });
                else
                    buttons[i].GetComponent<Button>().interactable = false;
            }
            else
            {
                buttons[i].GetComponent<Button>().interactable = false;
                buttons[i].GetComponent<Image>().sprite = emptySprite;
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = null;
                buttons[i].GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
    }

    void ResizePanel()
    {
        
        rectTransform = GetComponent<RectTransform>();
        cellSize = Screen.height * cellRatio * UIScale;
        float spacing = cellSize * spacingRatio;
        float padding = cellSize * paddingRatio;
        float panelWidth = (cellSize * 5) + (spacing * 4) + (padding * 2);
        float panelHeight = (cellSize * 3) + (spacing * 2) + (padding * 2);

        rectTransform.sizeDelta = new Vector2(panelWidth+0.5f, panelHeight+0.5f);
        GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellSize, cellSize);
        GetComponent<GridLayoutGroup>().spacing = new Vector2(spacing, spacing);
        GetComponent<GridLayoutGroup>().padding.left = (int)padding;
        GetComponent<GridLayoutGroup>().padding.right = (int)padding;
        GetComponent<GridLayoutGroup>().padding.top = (int)padding;
        GetComponent<GridLayoutGroup>().padding.bottom = (int)padding;

        foreach (GameObject button in buttons)
            button.GetComponentInChildren<TextMeshProUGUI>().fontSize = cellSize * 0.37662f;
    }

    #region structs
    public struct CommandButtonData
    {
        public bool isFilled;
        public KeyCode shortcut;
        public Func<Game.Commands.Command> command;

    }
    #endregion
}
