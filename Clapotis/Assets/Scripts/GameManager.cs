using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Clapotis
{
	using TMPro;
	using UnityEngine;

	[Serializable]
	public class Location
	{
		// -1 = Temple, other = region
		public int	regionIndex = -1;
		public int	spotIndex;

		public void	GoToTemple()
		{
			this.regionIndex = -1;
			this.spotIndex = -1;
		}
	}

	[Serializable]
	public class Spot
	{
		public int	index;
	}

	[Serializable]
	public class Region
	{
		public readonly int			index;
		public readonly List<Spot>	spots = new List<Spot>();
	}

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
	}

	[Serializable]
	public class Player
	{
		public readonly List<Artefact>	artefacts = new List<Artefact>();
		public readonly Location		location = new Location();

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

		public float	newArtefactRate = .5F;
		public float	newMonsterRate = .15F;

		public Board(int playersCount, int artefactsCount, int regionsCount = 6, int spotPerRegion = 4)
		{
			this.regionsCount = regionsCount;
			this.spotPerRegion = spotPerRegion;

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
			while (this.GetItemOnLocation(location.regionIndex, location.spotIndex) != null);

			return location;
		}
		
		public OnBoardItem	GetItemFromLocation(Location location)
		{
			for (int i = 0; i < this.boardItems.Count; i++)
			{
				if (this.boardItems[i].location == location)
					return this.boardItems[i];
			}

			return null;
		}

		public Location GetRandomArtefactLocation()
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

		public Item	GetItemOnLocation(int regionIndex, int spotIndex)
		{
			for (int i = 0; i < this.boardItems.Count; i++)
			{
				if (this.boardItems[i].location.regionIndex == regionIndex &&
					this.boardItems[i].location.spotIndex == spotIndex)
				{
					return this.boardItems[i].item;
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
		StartTurn = -1,
		PlayerIdle,
		AskPlayerSpot,
		SecondIdle,
		GodTurn
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
		public int	monsterPower;

		public UnityEvent	FirstIdleEvent;
		public UnityEvent	PlayersTurnEvent;
		public UnityEvent	SecondIdleEvent;
		public UnityEvent	GodTurnEvent;

		public UnityEvent<string>	confirmEvent;
		public UnityEvent<string>	messageEvent;

		public int	askRegionIndex;
		public int	askSpotIndex;

		public Image	idleTurnImage;

		[SerializeField]
		private GamePhase	gamePhase = GamePhase.StartTurn;
		[SerializeField]
		private Board		board;
		[SerializeField]
		private List<int>	turnOrders = new List<int>();
		[SerializeField]
		private int			currentTurn = -1;
		[SerializeField]
		private int			turnCounter = 0;

		public void	StartNewParty(int playersCount, int artefactsCount)
		{
			this.gamePhase = GamePhase.StartTurn;
			this.board = new Board(playersCount, artefactsCount);
			this.currentTurn = -1;
			this.turnCounter = 0;
			this.NextPhase();
		}

		public void	NextPhase()
		{
			switch (this.gamePhase)
			{
				case GamePhase.StartTurn:
					++this.turnCounter;
					this.GenerateTurnOrders(this.turnOrders);
					this.gamePhase = GamePhase.PlayerIdle;
					break;

				case GamePhase.PlayerIdle:
					if (this.turnOrders.Count == 0)
					{
						this.gamePhase = GamePhase.SecondIdle;
						this.NextPhase();
						break;
					}

					this.FirstIdleEvent.Invoke();

					this.currentTurn = this.turnOrders[0];
					this.idleTurnImage.color = GameManager.colors[this.currentTurn];
					this.turnOrders.RemoveAt(0);

					this.DisplayMessage("Player " + (this.currentTurn + 1), () =>
					{
						this.gamePhase = GamePhase.AskPlayerSpot;
						this.NextPhase();
					});
					break;

				case GamePhase.AskPlayerSpot:
					//this.PlayersTurnEvent.Invoke();

					Player	player = this.board.players[this.currentTurn];
					Item	item = this.board.GetItemOnLocation(this.askRegionIndex, this.askSpotIndex);

					player.location.regionIndex = this.askRegionIndex;
					player.location.spotIndex = this.askSpotIndex;

					if (item != null)
					{
						if (item.type == Item.Type.Monster)
						{
							Monster	monster = (item as Monster);
							int		power = monster.power;

							this.DisplayConfirm("Monster has a power of " + power + ".\nDid you win?", isYes =>
							{
								if (isYes == true)
								{
									player.artefacts.Add(monster.artefact);
									this.board.DeleteItemOnLocation(this.askRegionIndex, this.askSpotIndex);

									this.DisplayMessage("You received a new artefact!", () =>
									{
										this.gamePhase = GamePhase.PlayerIdle;
										this.NextPhase();
									});
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
						else if (item.type == Item.Type.Artefact)
						{
							player.artefacts.Add(item as Artefact);
							this.board.DeleteItemOnLocation(this.askRegionIndex, this.askSpotIndex);

							this.DisplayMessage("You received a new artefact!", () =>
							{
								this.gamePhase = GamePhase.PlayerIdle;
								this.NextPhase();
							});
						}
					}
					else
					{
						this.DisplayMessage("No item found, here is your clue.", () =>
						{
							this.gamePhase = GamePhase.PlayerIdle;
							this.NextPhase();
						});
					}

					break;

				case GamePhase.SecondIdle:
					this.SecondIdleEvent.Invoke();

					this.idleTurnImage.color = Color.cyan;

					this.DisplayMessage("The True God Amongst All's turn", () =>
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
								this.board.boardItems.Add(new OnBoardItem(artefact, location));
						}
					}
					else if (Random.Range(0F, 1F) > this.board.newMonsterRate)
					{
						Location	location = this.board.GetRandomArtefactLocation();

						if (location != null)
						{
							OnBoardItem	boardItem =  this.board.GetItemFromLocation(location);

							boardItem.item = new Monster(this.monsterPower, boardItem.item as Artefact);
						}
					}

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
							this.gamePhase = GamePhase.StartTurn;
							this.NextPhase();
						});
					}

					break;
			}
		}

		public void GenerateTurnOrders(List<int> playersTurns)
		{
			playersTurns.Clear();

			for (int i = 0; i < this.board.players.Count; i++)
			{
				int	myPower = this.board.players[i].CalculatePower();

				for (int j = 0; j < playersTurns.Count; j++)
				{
					int	hisPower = this.board.players[playersTurns[j]].CalculatePower();

					if (myPower >= hisPower)
					{
						if (myPower > hisPower || (myPower == hisPower && Random.Range(0F, 1F) > .5F))
							playersTurns.Insert(j, i);
						else
							playersTurns.Insert(j + 1, i);
					}
					else
						playersTurns.Add(i);
				}
			}
		}

		public GameObject	messagePopup;
		public Action		messageCallback;

		public void	DisplayMessage(string message, Action callback)
		{
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