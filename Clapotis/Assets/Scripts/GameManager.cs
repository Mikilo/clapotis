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
			Weapon,
			Shield,
			Total
		}

		public readonly int		group;
		public readonly Slot	slot;

		public	Artefact(int group, Slot slot) : base(Item.Type.Artefact)
		{
			this.group = group;
			this.slot = slot;
		}

		public static implicit operator int(Artefact value)
		{
			return value.group * (int)value.slot;
		}

		public override string	ToString()
		{
			return "Artefact " + slot + " " + this.group;
		}
	}

	[Serializable]
	public class Monster : Item
	{
		public Artefact	artefact;

		public	Monster(Artefact artefact) : base(Type.Monster)
		{
			this.artefact = artefact;
		}

		public override string	ToString()
		{
			return "Monster with " + this.artefact;
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

	public class Board
	{
		public readonly List<Player>		players = new List<Player>();
		public readonly List<Artefact>		artefacts = new List<Artefact>();
		public readonly List<OnBoardItem>	boardItems = new List<OnBoardItem>();
		public readonly Player				god;

		public readonly List<Artefact>	firstSacrifices = new List<Artefact>();
		public readonly List<Artefact>	lastSacrifices = new List<Artefact>();

		public readonly int	regionsCount;
		public readonly int	spotPerRegion;

		public float	newArtefactRate = .5F;
		public float	newMonsterRate = .15F;

		public int	winner;
		public int	gameIteration;

		public	Board(int playersCount, int artefactsCount, Board board, int spotPerRegion = 4)
		{
			this.regionsCount = playersCount + 2;
			this.spotPerRegion = spotPerRegion;

			this.players = new List<Player>(playersCount);
			for (int i = 0; i < playersCount; i++)
				this.players.Add(new Player());

			this.artefacts = new List<Artefact>(28);

			if (board != null)
			{
				this.gameIteration = board.gameIteration + 1;
				this.firstSacrifices.AddRange(board.firstSacrifices);
				this.lastSacrifices.AddRange(board.lastSacrifices);

				for (int i = 0; i < this.firstSacrifices.Count; i++)
				{
					if (this.firstSacrifices[i].slot == Artefact.Slot.Shield)
						this.newMonsterRate += .2F;
				}

				for (int i = 0; i < board.players.Count; i++)
				{
					for (int j = 0; j < board.players[i].artefacts.Count; j++)
						this.artefacts.Add(board.players[i].artefacts[j]);
				}

				for (int i = 0; i < board.boardItems.Count; i++)
				{
					if (board.boardItems[i].item.type == Item.Type.Artefact)
						this.artefacts.Add(board.boardItems[i].item as Artefact);
					else if (board.boardItems[i].item.type == Item.Type.Monster)
						this.artefacts.Add((board.boardItems[i].item as Monster).artefact);
				}

				List<Artefact>	localArtefacts = new List<Artefact>();

				for (int i = 0; i < this.regionsCount; i++)
				{
					localArtefacts.Clear();

					for (int j = 0; j < board.players.Count; j++)
					{
						if (board.players[j].location.regionIndex == i)
						{
							for (int k = 0; k < board.players[j].artefacts.Count;  k++)
								localArtefacts.Insert(Random.Range(0, localArtefacts.Count + 1), board.players[j].artefacts[k]);
						}
					}

					// Fill the region.
					if (localArtefacts.Count >= this.spotPerRegion)
					{
						for (int j = 0; j < localArtefacts.Count && j < this.spotPerRegion; j++)
						{
							Location	location = new Location() { regionIndex = i, spotIndex = j };

							this.boardItems.Add(new OnBoardItem(localArtefacts[j], location));
							this.artefacts.Remove(localArtefacts[j]);
						}
					}
					else
					{
						for (int j = 0; j < localArtefacts.Count; j++)
						{
							Location	location = new Location() { regionIndex = i, spotIndex = Random.Range(0, 4) };

							do
							{
								location.spotIndex = Random.Range(0, 4);
							}
							while (this.GetBoardItemFromLocation(i, location.spotIndex) != null);

							this.boardItems.Add(new OnBoardItem(localArtefacts[j], location));
							this.artefacts.Remove(localArtefacts[j]);
						}
					}
				}
			}
			else
			{
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
		SelectPlayers,
		PickCards,
		MoveAndBattlePhase,
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
		public int	maxWinStreak = 6;

		public UnityEvent	SelectPlayersEvent;
		public UnityEvent	FirstIdleEvent;
		public UnityEvent	AskPlayerSpotEvent;
		public UnityEvent	ConfirmPlayerSpotEvent;
		public UnityEvent	WatchClueEvent;
		public UnityEvent	SecondIdleEvent;
		public UnityEvent	GodTurnEvent;
		public UnityEvent	EndGameEvent;
		public UnityEvent	FinalizeBoardEvent;

		public UnityEvent	PromptArtefactEvent;

		public UnityEvent<string>	confirmEvent;
		public UnityEvent<string>	messageEvent;

		public int	askRegionIndex;
		public int	askSpotIndex;

		public TextMeshProUGUI	firstSacrificeDescription;
		public Selector	lastSelector;
		public int	lastChoice;

		public GameObject	playerModel;
		public GameObject	artefactClueModel;
		public GameObject	monsterClueModel;
		public Image		idleTurnImage;
		public GameObject	godSpot;
		public GameObject[]	spots;
		public Texture2D[]	icons;
		public Sprite[]		icons2;

		public AudioRandomizer	playGodCreatesArtifact;
		public AudioRandomizer	playGodCreatesMonster;
		public AudioRandomizer	playGodDefeated;
		public AudioRandomizer	playNewPlayerTurn;
		public AudioRandomizer	playPlayerDefeatedByGod;
		public AudioRandomizer	playPlayerDefeatedByMonster;
		public AudioRandomizer	playPlayerDefeatsMonster;
		public AudioRandomizer	playPlayerSacrificesArtifact;
		public AudioRandomizer	playPlayerFoundArtifact;

		[SerializeField]
		public GamePhase	gamePhase;
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
				for (int i = 0; i < this.board.players[this.currentPlayer].artefacts.Count; i++)
					GUILayout.Label("Artefact[" + i + "]: " + this.board.players[this.currentPlayer].artefacts[i]);

				for (int i = 0; i < this.board.players.Count; i++)
					GUILayout.Label("Player[" + i + " " + this.board.players[i].location + "]: Power=" + this.board.players[i].CalculatePower() + " Artfs=" + this.board.players[i].artefacts.Count);

				GUILayout.Label("Items on board: " + this.board.boardItems.Count);
				for (int i = 0; i < this.board.boardItems.Count; i++)
					GUILayout.Label("Item[" + i + " " + this.board.boardItems[i].location + "]: " + this.board.boardItems[i].item);
			}
		}

		public void	SetLastChoice(int choice)
		{
			this.playPlayerSacrificesArtifact.PlayRandom(true);
			this.lastChoice = choice;
		}

		public void	FinalizeBoard()
		{
			Player	player = this.board.players[this.turnOrders[this.turnOrders.Count - 1]];

			for (int i = 0; i < player.artefacts.Count; i++)
			{
				if ((int)player.artefacts[i].slot == this.lastChoice)
				{
					this.board.lastSacrifices.Add(player.artefacts[i]);
					player.artefacts.RemoveAt(i);
					break;
				}
			}

			this.FinalizeBoardEvent.Invoke();
		}

		public void SetPlayers(int count)
		{
			this.playersCount = count;
			this.StartNewParty();
		}

		public void	StartNewParty()
		{
			this.StartNewParty(this.playersCount, this.artefactsCount);
		}

		public void	StartNewParty(int playersCount, int artefactsCount)
		{
			for (int i = 0; i < this.spots.Length; i++)
				this.spots[i].SetActive(i < (playersCount + 2) * 4);

			this.gamePhase = GamePhase.PickCards;
			this.board = new Board(playersCount, artefactsCount, this.board);
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
				case GamePhase.SelectPlayers:
					this.SelectPlayersEvent.Invoke();
					break;

				case GamePhase.PickCards:
					++this.turnCounter;
					this.GenerateTurnOrders(this.turnOrders);

					this.DisplayMessage("Piochez vos cartes de déplacement", "Débutez", () =>
					{
						this.gamePhase = GamePhase.MoveAndBattlePhase;
						this.NextPhase();
					});
					break;

				case GamePhase.MoveAndBattlePhase:
					this.DisplayMessage("Exécutez vos déplacements et résolvez les combats", "Poursuivez", () =>
					{
						this.gamePhase = GamePhase.PlayerIdle;
						this.NextPhase();
					});
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
					this.idleTurnImage.GetComponentsInChildren<Image>()[1].color = GameManager.colors[this.currentPlayer];
					this.turnOrders.RemoveAt(0);

					// TODO Ajoutez l'image
					//this.idleTurnImage.gameObject.SetActive(true);
					//this.DisplayMessage("Débutez votre tour " + this.currentPlayer, "Débutez", () =>
					//{
					//	//this.idleTurnImage.gameObject.SetActive(false);
						this.gamePhase = GamePhase.AskPlayerSpot;
					//	this.NextPhase();
					//});

					this.playNewPlayerTurn.PlayRandom();

					break;

				case GamePhase.AskPlayerSpot:
					Location	location = this.board.players[this.currentPlayer].location;

					Debug.Log(this.currentPlayer + " is at " + location);
					if (location.regionIndex == -1)
					{
					}
					else
						Object.FindObjectOfType<DragAndDropPlayer>().originPosition = this.spots[location.regionIndex * this.board.spotPerRegion + location.spotIndex].transform.position + Vector3.up;

					this.AskPlayerSpotEvent.Invoke();
					break;

				case GamePhase.ConfirmPlayerSpot:
					this.ConfirmPlayerSpotEvent.Invoke();

					Player		player = this.board.players[this.currentPlayer];

					if (this.askRegionIndex == -2) // Fighting god.
					{
						this.DisplayConfirm("La montagne sacrée! Combattez", "Succès", "Échec", isYes =>
						{
							if (isYes == true)
							{
								this.playGodDefeated.PlayRandom();
								this.gamePhase = GamePhase.EndGame;
								this.NextPhase();
							}
							else
							{
								player.location.GoToTemple();

								this.playPlayerDefeatedByGod.PlayRandom();

								this.DisplayMessage("Retournez dans votre temple (sans récolter de cartes)", "Poursuivez", () =>
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

								this.DisplayConfirm("Un monstre! Combattez!", "Succès", "Échec", isYes =>
								{
									if (isYes == true)
									{
										this.playPlayerDefeatsMonster.PlayRandom();
										this.ProcessArtefact(player, boardItem, monster.artefact);
									}
									else
									{
										this.playPlayerDefeatedByMonster.PlayRandom();
										player.location.GoToTemple();

										this.DisplayMessage("Retournez dans votre temple (sans récolter de cartes)", "Poursuivez", () =>
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
							this.DisplayMessage("Pigez une carte de malédiction et donnez-la face cachée à un autre joueur", "Continuez", () =>
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

					this.StopAllCoroutines();
					this.StartCoroutine(this.HighlightClue());
					break;

				case GamePhase.SecondIdle:
					this.SecondIdleEvent.Invoke();

					this.DisplayMessage("La montagne sacrée s'agite", "Poursuivez", () =>
					{
						this.gamePhase = GamePhase.GodTurn;
						this.NextPhase();
					});

					break;

				case GamePhase.GodTurn:
					this.GodTurnEvent.Invoke();

					if (Random.Range(0F, 1F) > this.board.newMonsterRate)
					{
						Location	location2 = new Location()
						{
							regionIndex = Random.Range(0, this.board.regionsCount),
							spotIndex = Random.Range(0, this.board.spotPerRegion)
						};

						OnBoardItem	boardItem3 = this.board.GetBoardItemFromLocation(location2.regionIndex, location2.spotIndex);

						if (boardItem3 != null)
						{
							if (boardItem3.item.type == Item.Type.Artefact)
							{
								this.playGodCreatesMonster.PlayRandom();

								boardItem3.item = new Monster(boardItem3.item as Artefact);

								this.DisplayMessage("Un monstre est apparu!", "Poursuivez", () =>
								{
									this.gamePhase = GamePhase.PickCards;
									this.NextPhase();
								});
							}
						}
						else
						{
							Artefact	artefact = this.board.PopRandomArtefactFromPool();

							if (artefact != null)
							{
								this.playGodCreatesArtifact.PlayRandom();
								this.board.boardItems.Add(new OnBoardItem(artefact, location2));

								this.DisplayMessage("Un artéfact est apparu!", "Poursuivez", () =>
								{
									this.gamePhase = GamePhase.PickCards;
									this.NextPhase();
								});
							}
						}
					}
					else if (Random.Range(0F, 1F) > this.board.newArtefactRate)
					{
						Artefact	artefact = this.board.PopRandomArtefactFromPool();

						if (artefact != null)
						{
							Location	location2 = this.board.GetEmptyLocation();

							if (location2 != null)
							{
								this.playGodCreatesArtifact.PlayRandom();
								this.board.boardItems.Add(new OnBoardItem(artefact, location2));

								this.DisplayMessage("Un artéfact est apparu!", "Poursuivez", () =>
								{
									this.gamePhase = GamePhase.PickCards;
									this.NextPhase();
								});
							}
						}
					}
					else
					{
						this.DisplayMessage("Le royaume est paisible (on entend des crickets)", "Poursuivez", () =>
						{
							this.gamePhase = GamePhase.PickCards;
							this.NextPhase();
						});
					}

					break;

				case GamePhase.EndGame:
					this.GenerateTurnOrders(this.turnOrders);

					this.board.winner = this.currentPlayer;
					// Make sure the winner is not counted.
					this.turnOrders.Remove(this.currentPlayer);

					Player	first = this.board.players[this.currentPlayer];

					if (first.artefacts.Count > 0)
					{
						int	n;

						do
						{
							n = Random.Range(0, first.artefacts.Count);

							int	total = 0;

							for (int i = 0; i < this.board.firstSacrifices.Count; i++)
							{
								if (this.board.firstSacrifices[i].slot == first.artefacts[n].slot)
									++total;
							}

							if (total < 4)
								break;

							if (first.artefacts.Count == 1)
								goto comboBREAKER;
						}
						while (true);

						if (first.artefacts[n].slot == Artefact.Slot.Head)
							this.firstSacrificeDescription.text = "Tête: Les joueurs débutent la partie avec -1 cartes en main (max -4)";
						else if (first.artefacts[n].slot == Artefact.Slot.Body)
							this.firstSacrificeDescription.text = "Armure: Certains sites requièrent +1 carte de déplacement pour être atteints (max + 1 / site). Un collant est apposé à coté des sites en question.";
						else if (first.artefacts[n].slot == Artefact.Slot.Shield)
							this.firstSacrificeDescription.text = "Bouclier: Les probabilités d’apparition des monstres sont augmentées de 20 % (max + 80 %)";
						else if (first.artefacts[n].slot == Artefact.Slot.Weapon)
							this.firstSacrificeDescription.text = "Arme: La difficulté des monstres est augmentée de +1 niveau de puissance (max +4)";

						this.board.firstSacrifices.Add(first.artefacts[n]);
						first.artefacts.RemoveAt(n);
					}

					comboBREAKER:

					Player	last = this.board.players[this.turnOrders[this.turnOrders.Count - 1]];

					if (last.artefacts.Count > 0)
					{
						for (int i = 0; i < this.lastSelector.choices.Length; i++)
							this.lastSelector.choices[i].gameObject.SetActive(false);

						for (int i = 0; i < last.artefacts.Count; i++)
						{
							string	slot = null;

							if (last.artefacts[i].slot == Artefact.Slot.Head)
								slot = "Tête";
							else if (last.artefacts[i].slot == Artefact.Slot.Body)
								slot = "Armure";
							else if (last.artefacts[i].slot == Artefact.Slot.Weapon)
								slot = "Arme";
							else if (last.artefacts[i].slot == Artefact.Slot.Shield)
								slot = "Bouclier";

							this.lastSelector.choices[(int)last.artefacts[i].slot].gameObject.SetActive(true);
							this.lastSelector.choices[(int)last.artefacts[i].slot].GetComponentInChildren<Text>().text = slot;
						}

						this.firstTerminezButton.SetActive(false);
						this.firstContinuezButton.SetActive(true);
					}
					else
					{
						this.firstTerminezButton.SetActive(true);
						this.firstContinuezButton.SetActive(false);
					}

					this.EndGameEvent.Invoke();
					break;
			}
		}

		public GameObject	firstTerminezButton;
		public GameObject	firstContinuezButton;

		public Image	highlightClueFader;

		private IEnumerator	HighlightClue()
		{
			float	time = 0F;

			while (time < 5F)
			{
				time += Time.deltaTime;
				yield return null;
			}

			time = 0F;

			this.highlightClueFader.gameObject.SetActive(true);

			while (time < 1F)
			{
				time += Time.deltaTime;
				this.highlightClueFader.color = new Color(0F, 0F, 0F, time);
				yield return null;
			}

			this.highlightClueFader.gameObject.SetActive(false);
			this.artefactClueModel.SetActive(false);
			this.monsterClueModel.SetActive(false);

			this.gamePhase = GamePhase.PlayerIdle;
			this.NextPhase();
		}

		public Image	replaceArtefactImage;

		private void	ProcessArtefact(Player player, OnBoardItem boardItem, Artefact artefact)
		{
			if (player.CanAddArtefact(artefact) == false)
			{
				//this.replaceArtefactImage.sprite = null;
				this.replaceArtefactImage.GetComponentsInChildren<TextMeshProUGUI>()[1].text = artefact.ToString();

				this.promptPlayer = player;
				this.promptBoardItem = boardItem;
				this.promptArtefact = artefact;

				this.PromptArtefactEvent.Invoke();
			}
			else
				this.AddArtefactToPlayer(player, artefact, boardItem);
		}

		public Image	foundArtefact;
		public Image	replaceArtefact;

		public void	RunPlayerIdlePhase()
		{
			this.gamePhase = GamePhase.PlayerIdle;
			this.NextPhase();
		}

		private void	AddArtefactToPlayer(Player player, Artefact artefact, OnBoardItem boardItem)
		{
			Artefact	replacedArtefact = player.AddOrReplaceArtefact(artefact);

			if (replacedArtefact == null)
				this.board.DeleteItemOnLocation(this.askRegionIndex, this.askSpotIndex);
			else
				boardItem.item = replacedArtefact;

			if (replacedArtefact != null)
			{
				this.playPlayerFoundArtifact.PlayRandom();
				this.foundArtefact.sprite = this.icons2[artefact];
				this.foundArtefact.transform.parent.gameObject.SetActive(true);

				//this.DisplayMessage("Vous avez trouvé " + artefact + " et déposé " + replacedArtefact + "!", "Continuez", () =>
				//{
				//	this.gamePhase = GamePhase.PlayerIdle;
				//	this.NextPhase();
				//});
			}
			else
			{
				this.playPlayerFoundArtifact.PlayRandom();
				this.replaceArtefact.sprite = this.icons2[artefact];
				this.replaceArtefact.transform.parent.gameObject.SetActive(true);

				//this.DisplayMessage("Vous avez trouvé " + artefact + "!", "Récupérez", () =>
				//{
				//	this.gamePhase = GamePhase.PlayerIdle;
				//	this.NextPhase();
				//});
			}
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

		private Player		promptPlayer;
		private OnBoardItem	promptBoardItem;
		private Artefact	promptArtefact;

		public void	ReplaceArtefact(bool answer)
		{
			if (answer == true)
				this.AddArtefactToPlayer(promptPlayer, promptArtefact, promptBoardItem);
			else
			{
				this.DisplayMessage("Vous avez abandonné l'artéfact sur place!", "Continuez", () =>
				{
					this.gamePhase = GamePhase.PlayerIdle;
					this.NextPhase();
				});
			}
		}

		public GameObject	messagePopup;
		public Action		messageCallback;

		public void	DisplayMessage(string message, string button, Action callback)
		{
			Debug.Log("DisplayMessage(" + message + ")");
			this.messagePopup.SetActive(true);
			this.messagePopup.GetComponentInChildren<TextMeshProUGUI>().text = message;
			this.messagePopup.GetComponentInChildren<Text>().text = button;
			this.messageCallback = callback;
		}

		public void	MessageRead()
		{
			this.messageCallback();
		}

		public GameObject	confirmPopup;
		public Action<bool>	confirmCallback;

		public void	DisplayConfirm(string message, string buttonYes, string buttonNo, Action<bool> callback)
		{
			this.confirmPopup.SetActive(true);
			this.confirmPopup.GetComponentInChildren<TextMeshProUGUI>().text = message;
			this.confirmPopup.GetComponentsInChildren<Text>()[0].text = buttonYes;
			this.confirmPopup.GetComponentsInChildren<Text>()[1].text = buttonNo;
			this.confirmCallback = callback;
		}

		public void	Confirm(bool answer)
		{
			this.confirmCallback(answer);
		}
	}
}