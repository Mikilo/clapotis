using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Clapotis
{
	using System.Collections;
	using TMPro;
	using UnityEngine;

	[Serializable]
	public class Location
	{
		// -2 = god, -1 = temple, >=0 = region
		public int	regionIndex = -1;
		public int	spotIndex;

		public void	GoToTemple()
		{
			this.regionIndex = -1;
			this.spotIndex = -1;
		}

		public override string	ToString()
		{
			return this.regionIndex + ";" + this.spotIndex;
		}
	}

	//[Serializable]
	//public class Spot
	//{
	//	public int	index;
	//}

	//[Serializable]
	//public class Region
	//{
	//	public readonly int			index;
	//	public readonly List<Spot>	spots = new List<Spot>();
	//}

	[Serializable]
	public abstract class Item
	{
		public enum Type
		{
			Monster,
			Artefact
		}

		public readonly Type	type;

		public	Item(Type type)
		{
			this.type = type;
		}
	}

	[Serializable]
	public class Artefact : Item
	{
		public enum Slot
		{
			Head,
			Body,
			LeftArm,
			RightArm,
			Total
		}

		public readonly int		group;
		public readonly Slot	slot;

		public	Artefact(int group, Slot slot) : base(Item.Type.Artefact)
		{
			this.group = group;
			this.slot = slot;
		}

		public override string	ToString()
		{
			return "Artefact " + slot + " " + this.group;
		}
	}

	[Serializable]
	public class Monster : Item
	{
		public int		power;
		public Artefact	artefact;

		public	Monster(int power, Artefact artefact) : base(Type.Monster)
		{
			this.power = power;
			this.artefact = artefact;
		}

		public override string	ToString()
		{
			return "Monster " + power + " " + this.artefact;
		}
	}

	[Serializable]
	public class Player
	{
		public readonly List<Artefact>	artefacts = new List<Artefact>();
		public readonly Location		location = new Location();

		public bool	CanAddArtefact(Artefact artefact)
		{
			for (int i = 0; i < this.artefacts.Count; i++)
			{
				if (this.artefacts[i].slot == artefact.slot)
					return false;
			}

			return true;
		}

		public Artefact	AddOrReplaceArtefact(Artefact artefact)
		{
			for (int i = 0; i < this.artefacts.Count; i++)
			{
				if (this.artefacts[i].slot == artefact.slot)
				{
					Artefact	tmp = this.artefacts[i];
					this.artefacts[i] = artefact;
					return tmp;
				}
			}

			this.artefacts.Add(artefact);

			return null;
		}

		public int	CalculatePower()
		{
			int	power = 0;

			for (int i = 0; i < this.artefacts.Count; i++)
			{
				if (i > 0)
				{
					for (int j = i - 1; j >= 0; j--)
					{
						if (this.artefacts[j] == this.artefacts[i])
						{
							++power;
							// TODO comment next line for continuous power-up by set of more than 2.
							break;
						}
					}
				}

				++power;
			}

			return power;
		}
	}

	[Serializable]
	public class OnBoardItem
	{
		public Item					item;
		public readonly Location	location;

		public	OnBoardItem(Item item, Location location)
		{
			this.item = item;
			this.location = location;
		}

		public override string	ToString()
		{
			return this.item + " " + this.location;
		}
	}

	[Serializable]
	public class Board
	{
		public readonly List<Player>		players = new List<Player>();
		public readonly List<Artefact>		artefacts = new List<Artefact>();
		public readonly List<OnBoardItem>	boardItems = new List<OnBoardItem>();
		public readonly Player				god;

		public readonly int	regionsCount;
		public readonly int	spotPerRegion;
		public readonly int	monsterPower;
		public readonly int	godPower;

		public float	newArtefactRate = .5F;
		public float	newMonsterRate = .15F;

		public int	gameIteration;

		public	Board(Board board)
		{
			this.gameIteration = board.gameIteration + 1;
		}

		public	Board(int playersCount, int artefactsCount, int monsterPower, int regionsCount = 6, int spotPerRegion = 4)
		{
			this.regionsCount = regionsCount;
			this.spotPerRegion = spotPerRegion;
			this.monsterPower = monsterPower;

			this.players = new List<Player>(playersCount);
			for (int i = 0; i < playersCount; i++)
				this.players.Add(new Player());

			this.artefacts = new List<Artefact>(28);
			for (int i = 0; i < 28; i++)
				this.artefacts.Add(new Artefact(i / 2, (Artefact.Slot)((i % (int)Artefact.Slot.Total) & 0x3)));

			this.boardItems = new List<OnBoardItem>(artefactsCount);
			for (int i = 0; i < artefactsCount; i++)
			{
				int			randomArtefactIndex = Random.Range(0, this.artefacts.Count);
				Location	location = this.GetEmptyLocation();

				this.boardItems.Add(new OnBoardItem(this.artefacts[randomArtefactIndex], location));
				this.artefacts.RemoveAt(randomArtefactIndex);
			}
		}

		public Artefact	PopRandomArtefactFromPool()
		{
			if (this.artefacts.Count > 0)
			{
				int	i = Random.Range(0, this.artefacts.Count);

				Artefact	artefact = this.artefacts[i];
				this.artefacts.RemoveAt(i);
				return artefact;
			}

			return null;
		}

		public Location	GetEmptyLocation()
		{
			if (this.boardItems.Count >= this.regionsCount * this.spotPerRegion)
				return null;

			Location	location = new Location();

			do
			{
				location.regionIndex = Random.Range(0, this.regionsCount);
				location.spotIndex = Random.Range(0, this.spotPerRegion);
			}
			while (this.GetBoardItemFromLocation(location.regionIndex, location.spotIndex) != null);

			return location;
		}

		public Location		GetRandomArtefactLocation()
		{
			List<int>	artefactsAvailable = new List<int>();

			for (int i = 0; i < this.boardItems.Count; i++)
			{
				if (this.boardItems[i].item.type == Item.Type.Artefact)
					artefactsAvailable.Add(i);
			}

			if (artefactsAvailable.Count > 0)
				return this.boardItems[artefactsAvailable[Random.Range(0, artefactsAvailable.Count)]].location;
			return null;
		}

		public OnBoardItem	GetBoardItemFromLocation(int regionIndex, int spotIndex)
		{
			for (int i = 0; i < this.boardItems.Count; i++)
			{
				if (this.boardItems[i].location.regionIndex == regionIndex &&
					this.boardItems[i].location.spotIndex == spotIndex)
				{
					return this.boardItems[i];
				}
			}

			return null;
		}

		public void	DeleteItemOnLocation(int regionIndex, int spotIndex)
		{
			for (int i = 0; i < this.boardItems.Count; i++)
			{
				if (this.boardItems[i].location.regionIndex == regionIndex &&
					this.boardItems[i].location.spotIndex == spotIndex)
				{
					this.boardItems.RemoveAt(i);
					break;
				}
			}
		}
	}

	public enum GamePhase
	{
		MoveAndBattlePhase,
		StartTurn,
		PlayerIdle,
		AskPlayerSpot,
		ConfirmPlayerSpot,
		WatchClue,
		SecondIdle,
		GodTurn,
		EndGame
	}

	public class GameManager : MonoBehaviour
	{
		public static Color[]	colors = new Color[]
		{
			Color.red,
			Color.green,
			Color.blue,
			Color.yellow,
		};

		public int	playersCount;
		public int	artefactsCount;
		public int	defaultMonsterPower;
		public int	maxWinStreak = 6;

		public UnityEvent	FirstIdleEvent;
		public UnityEvent	AskPlayerSpotEvent;
		public UnityEvent	ConfirmPlayerSpotEvent;
		public UnityEvent	WatchClueEvent;
		public UnityEvent	SecondIdleEvent;
		public UnityEvent	GodTurnEvent;

		public UnityEvent<string>	confirmEvent;
		public UnityEvent<string>	messageEvent;

		public int	askRegionIndex;
		public int	askSpotIndex;

		public GameObject	playerModel;
		public GameObject	artefactClueModel;
		public GameObject	monsterClueModel;
		public Image		idleTurnImage;
		public GameObject	godSpot;
		public GameObject[]	spots;

		[SerializeField]
		public GamePhase	gamePhase = GamePhase.StartTurn;
		[SerializeField]
		private Board		board;
		public Board		Board { get { return this.board; } }
		[SerializeField]
		private List<int>	turnOrders = new List<int>();
		[SerializeField]
		private int			currentPlayer = -1;
		[SerializeField]
		private int			turnCounter = 0;

		private void	OnGUI()
		{
			if (this.currentPlayer >= 0)
			{
				GUILayout.Label("Current player: " + this.currentPlayer);
				for (int i = 0; i < this.board.players.Count; i++)
					GUILayout.Label("Player[" + i + "]: " + this.board.players[i].artefacts.Count);
				GUILayout.Label("Items on board: " + this.board.boardItems.Count);
				for (int i = 0; i < this.board.boardItems.Count; i++)
					GUILayout.Label("Item[" + i + " " + this.board.boardItems[i].location + "]: " + this.board.boardItems[i].item);
			}
		}

		public void	StartNewParty()
		{
			this.StartNewParty(this.playersCount, this.artefactsCount);
		}

		public void	StartNewParty(int playersCount, int artefactsCount)
		{
			this.gamePhase = GamePhase.MoveAndBattlePhase;
			this.board = new Board(playersCount, artefactsCount, this.defaultMonsterPower);
			this.currentPlayer = -1;
			this.turnCounter = 0;
			this.NextPhase();
		}

		public void EvolveParty()
		{
			this.gamePhase = GamePhase.MoveAndBattlePhase;
			this.board = new Board(this.board);
			this.currentPlayer = -1;
			this.turnCounter = 0;
			this.NextPhase();
		}

		public void	RunPhase(GamePhase phase)
		{
			this.gamePhase = phase;
			this.NextPhase();
		}

		public void NextPhase()
		{
			switch (this.gamePhase)
			{
				case GamePhase.MoveAndBattlePhase:
					this.DisplayMessage("Press to initiate the first turn.", () =>
					{
						this.gamePhase = GamePhase.StartTurn;
						this.NextPhase();
					});
					break;

				case GamePhase.StartTurn:
					++this.turnCounter;
					this.GenerateTurnOrders(this.turnOrders);
					this.gamePhase = GamePhase.PlayerIdle;
					this.NextPhase();
					break;

				case GamePhase.PlayerIdle:
					if (this.turnOrders.Count == 0)
					{
						this.gamePhase = GamePhase.SecondIdle;
						this.NextPhase();
						break;
					}

					this.FirstIdleEvent.Invoke();

					this.currentPlayer = this.turnOrders[0];
					this.idleTurnImage.gameObject.SetActive(true);
					this.idleTurnImage.color = GameManager.colors[this.currentPlayer];
					this.turnOrders.RemoveAt(0);

					this.DisplayMessage("Player " + (this.currentPlayer + 1), () =>
					{
						this.idleTurnImage.gameObject.SetActive(false);
						this.gamePhase = GamePhase.AskPlayerSpot;
						this.NextPhase();
					});
					break;

				case GamePhase.AskPlayerSpot:
					this.AskPlayerSpotEvent.Invoke();
					break;

				case GamePhase.ConfirmPlayerSpot:
					this.ConfirmPlayerSpotEvent.Invoke();

					Player		player = this.board.players[this.currentPlayer];

					if (this.askRegionIndex == -2) // Fighting god.
					{
						this.DisplayConfirm("Michael has a power of " + this.board.godPower + ".\nDid you win?", isYes =>
						{
							if (isYes == true)
							{
								this.gamePhase = GamePhase.EndGame;
								this.NextPhase();
							}
							else
							{
								player.location.GoToTemple();

								this.DisplayMessage("You lost, back to temple...", () =>
								{
									this.gamePhase = GamePhase.PlayerIdle;
									this.NextPhase();
								});
							}
						});
					}
					else
					{
						OnBoardItem	boardItem = this.board.GetBoardItemFromLocation(this.askRegionIndex, this.askSpotIndex);

						player.location.regionIndex = this.askRegionIndex;
						player.location.spotIndex = this.askSpotIndex;

						if (boardItem != null)
						{
							if (boardItem.item.type == Item.Type.Monster)
							{
								Monster	monster = (boardItem.item as Monster);

								this.DisplayConfirm("Monster has a power of " + monster.power + ".\nDid you win?", isYes =>
								{
									if (isYes == true)
										this.ProcessArtefact(player, boardItem, monster.artefact);
									else
									{
										player.location.GoToTemple();

										this.DisplayMessage("You lost, back to temple...", () =>
										{
											this.gamePhase = GamePhase.PlayerIdle;
											this.NextPhase();
										});
									}
								});
							}
							else if (boardItem.item.type == Item.Type.Artefact)
								this.ProcessArtefact(player, boardItem, boardItem.item as Artefact);
						}
						else
						{
							this.DisplayMessage("Please \"pige une carte\" and please watch the following clue.", () =>
							{
								this.gamePhase = GamePhase.WatchClue;
								this.NextPhase();
							});
						}
					}

					break;

				case GamePhase.WatchClue:
					OnBoardItem	boardItem2 = this.board.boardItems[Random.Range(0, this.board.boardItems.Count)];

					if (boardItem2.item.type == Item.Type.Artefact)
					{
						this.artefactClueModel.SetActive(true);
						this.artefactClueModel.transform.localPosition = this.spots[boardItem2.location.regionIndex * this.board.spotPerRegion + boardItem2.location.spotIndex].transform.position + Vector3.up;
					}
					else if (boardItem2.item.type == Item.Type.Monster)
					{
						this.monsterClueModel.SetActive(true);
						this.monsterClueModel.transform.localPosition = this.spots[boardItem2.location.regionIndex * this.board.spotPerRegion + boardItem2.location.spotIndex].transform.position + Vector3.up;
					}

					this.WatchClueEvent.Invoke();

					this.StartCoroutine(this.HighlightClue());
					break;

				case GamePhase.SecondIdle:
					this.SecondIdleEvent.Invoke();

					this.idleTurnImage.color = Color.cyan;

					this.DisplayMessage("It is Michael Allmighty's turn", () =>
					{
						this.gamePhase = GamePhase.GodTurn;
						this.NextPhase();
					});

					break;

				case GamePhase.GodTurn:
					this.GodTurnEvent.Invoke();

					if (Random.Range(0F, 1F) > this.board.newArtefactRate)
					{
						Artefact	artefact = this.board.PopRandomArtefactFromPool();

						if (artefact != null)
						{
							Location	location = this.board.GetEmptyLocation();

							if (location != null)
							{
								this.board.boardItems.Add(new OnBoardItem(artefact, location));

								this.DisplayMessage("A new Artefact appeared !", () =>
								{
									this.gamePhase = GamePhase.MoveAndBattlePhase;
									this.NextPhase();
								});
							}
						}
					}
					else if (Random.Range(0F, 1F) > this.board.newMonsterRate)
					{
						Location	location = this.board.GetRandomArtefactLocation();

						if (location != null)
						{
							OnBoardItem	boardItem3 =  this.board.GetBoardItemFromLocation(location.regionIndex, location.spotIndex);

							boardItem3.item = new Monster(this.board.monsterPower, boardItem3.item as Artefact);

							this.DisplayMessage("A new monster arrived !", () =>
							{
								this.gamePhase = GamePhase.MoveAndBattlePhase;
								this.NextPhase();
							});
						}
					}
					else
					// TODO Random skill affecting the board
					//if ()
					//{
					//	this.DisplayMessage("Something happened", () =>
					//	{
					//		this.gamePhase = GamePhase.StartTurn;
					//		this.NextPhase();
					//	});
					//}
					//else
					{
						this.DisplayMessage("Nothing happened.", () =>
						{
							this.gamePhase = GamePhase.MoveAndBattlePhase;
							this.NextPhase();
						});
					}

					break;

				case GamePhase.EndGame:
					this.DisplayMessage("You won against Michael the Allmight... GG & Kudos to you !", () =>
					{
						if (this.board.gameIteration + 1 == this.maxWinStreak)
						{
							this.DisplayMessage("You reached " + this.maxWinStreak + " wins against Michael, congratulation !\nPress to start a new game", () =>
							{
								this.StartNewParty();
							});
						}
						else
						{
							this.DisplayMessage("Press to evolve to the New World", () =>
							{
								this.EvolveParty();
							});
						}
					});
					break;
			}
		}

		private IEnumerator	HighlightClue()
		{
			float	time = 0F;

			while (time < 5F)
			{
				time += Time.deltaTime;
				yield return null;
			}

			time = 0F;

			this.messagePopup.SetActive(true);
			this.messagePopup.GetComponentInChildren<TextMeshProUGUI>().text = "";

			while (time < 1F)
			{
				time += Time.deltaTime;
				this.messagePopup.GetComponent<CanvasGroup>().alpha = time;
				yield return null;
			}

			this.messagePopup.GetComponent<CanvasGroup>().alpha = 1F;

			this.artefactClueModel.SetActive(false);
			this.monsterClueModel.SetActive(false);

			this.gamePhase = GamePhase.PlayerIdle;
			this.NextPhase();
		}

		private void	ProcessArtefact(Player player, OnBoardItem boardItem, Artefact artefact)
		{
			if (player.CanAddArtefact(artefact) == false)
			{
				this.DisplayConfirm("You already possess a " + artefact.slot + " Artefact, replace with " + artefact + "?", isYes2 =>
				{
					if (isYes2 == true)
						this.AddArtefactToPlayer(player, artefact, boardItem);
				});
			}
			else
				this.AddArtefactToPlayer(player, artefact, boardItem);
		}

		private void	AddArtefactToPlayer(Player player, Artefact artefact, OnBoardItem boardItem)
		{
			artefact = player.AddOrReplaceArtefact(artefact);

			if (artefact == null)
				this.board.DeleteItemOnLocation(this.askRegionIndex, this.askSpotIndex);
			else
				boardItem.item = artefact;

			this.DisplayMessage("You received Artefact " + artefact + " !", () =>
			{
				this.gamePhase = GamePhase.PlayerIdle;
				this.NextPhase();
			});
		}

		public void GenerateTurnOrders(List<int> playersTurns)
		{
			playersTurns.Clear();

			for (int i = 0; i < this.board.players.Count; i++)
			{
				int	myPower = this.board.players[i].CalculatePower();
				int	j = 0;

				for (; j < playersTurns.Count; j++)
				{
					int	hisPower = this.board.players[playersTurns[j]].CalculatePower();

					if (myPower >= hisPower)
					{
						if (myPower > hisPower || (myPower == hisPower && Random.Range(0F, 1F) > .5F))
							playersTurns.Insert(j, i);
						else
							playersTurns.Insert(j + 1, i);
						break;
					}
				}
					
				if (j >= playersTurns.Count)
					playersTurns.Add(i);
			}
		}

		public GameObject	messagePopup;
		public Action		messageCallback;

		public void	DisplayMessage(string message, Action callback)
		{
			Debug.Log("DisplayMessage(" + message + ")");
			this.messagePopup.SetActive(true);
			this.messagePopup.GetComponentInChildren<TextMeshProUGUI>().text = message;
			this.messageCallback = callback;
		}

		public void	MessageRead()
		{
			this.messageCallback();
		}

		public GameObject	confirmPopup;
		public Action<bool>	confirmCallback;

		public void DisplayConfirm(string message, Action<bool> callback)
		{
			this.confirmPopup.SetActive(true);
			this.confirmPopup.GetComponentInChildren<TextMeshProUGUI>().text = message;
			this.confirmCallback = callback;
		}

		public void	Confirm(bool answer)
		{
			this.confirmCallback(answer);
		}
	}
}