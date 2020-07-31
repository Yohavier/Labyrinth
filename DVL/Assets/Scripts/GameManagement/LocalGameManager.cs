using UnityEngine;

public enum PlayerIndex
{
	Invalid = -1,
	Player1,
	Player2,
	Player3,
	Enemy
}

public class LocalGameManager : MonoBehaviour
{
	//Determines you playerChar
	public PlayerIndex localPlayerIndex;
	public bool isDebugingSolo = false;

	//Player index of current turn
	public PlayerIndex currentTurnPlayer;
	private PlayerIndex previousTurn;

	//reference to the Player of this gameInstance
	public GameObject activePlayer;

	public static LocalGameManager instance;

	//can move tile
	private bool moveTileToken;
	public bool _moveTileToken
    {
        get
        {
			return moveTileToken;
        }
        set
        {
			if (isDebugingSolo)
				moveTileToken = true;
			else
				moveTileToken = value;
        }
    }

	[HideInInspector] public bool canMove = true;
	//Steps you can take
	private int stepsLeft = 0;
	public int _stepsLeft
	{
		get { return stepsLeft; }
		set
		{ 
			if(currentTurnPlayer == localPlayerIndex)
            {
				if (isDebugingSolo)
					stepsLeft = 10;
				else
					stepsLeft = value;
				GUIManager.instance.stepsLeftLabel.text = stepsLeft.ToString();
				DiceHandler.instance.OnChangeDiceText(stepsLeft, false);
			}
		}
	}

	private void Awake()
	{
		instance = this;
		currentTurnPlayer = PlayerIndex.Invalid;
	}
    private void OnEnable()
    {
		Eventbroker.instance.onNotifyNextTurn += NotifyNextTurn;
    }
	private void OnDisable()
    {
		Eventbroker.instance.onNotifyNextTurn -= NotifyNextTurn;
	}
    private void Update()
    {
		if(previousTurn != currentTurnPlayer)
        {
			previousTurn = currentTurnPlayer;
			Eventbroker.instance.NotifyNextTurn();
        }
    }
	private void NotifyNextTurn()
    {
		if (GetTurn())
        {
			HandleRollDiceButton();
			_moveTileToken = true;
		}
        else
        {
			RemoveRollDiceButtonListener();
			_moveTileToken = false;
			if (activePlayer)
				activePlayer.GetComponent<Player>().NotifyNextTurn(false);
		}
    }
    public bool GetTurn()
	{
		return localPlayerIndex == currentTurnPlayer && GetInMatch();
	}
	public bool GetInMatch()
	{
		return localPlayerIndex != PlayerIndex.Invalid && currentTurnPlayer != PlayerIndex.Invalid;
	}


    #region Handle Roll Dice extension
    private void HandleRollDiceButton()
	{
		if (GUIManager.instance)
		{
			_stepsLeft = 0;
			GUIManager.instance.SetRollDiceButton(true);
			GUIManager.instance.rollDiceButton.onClick.AddListener(RollDice);
		}
	}
	private void RollDice()
    {
		GUIManager.instance.diceObject.SetActive(true);
		DiceHandler.instance.RollDiceAnimation(Random.Range(1, 7));
		RemoveRollDiceButtonListener();

		if(activePlayer)
			activePlayer.GetComponent<Player>().NotifyNextTurn(true);
    }
	private void RemoveRollDiceButtonListener()
    {
        if (GUIManager.instance)
		{
			GUIManager.instance.SetRollDiceButton(false);
			GUIManager.instance.rollDiceButton.onClick.RemoveAllListeners();
		}
	}
    #endregion
}
