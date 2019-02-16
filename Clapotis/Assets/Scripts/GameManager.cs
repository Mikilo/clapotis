using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Clapotis
{
	[Serializable]
	public class Location
	{
		// -1 = Temple, other = region
		public int	regionIndex = -1;
		public int	spotIndex;
	}

	[Serializable]
	public class Spot
	{
		public int	index;
	}

	[Serializable]
	public class Region
	{
		public int			index;
		public List<Spot>	locations = new List<Spot>();
	}

	[Serializable]
	public class Artefact
	{
		public int	group;
	}

	[Serializable]
	public class Player
	{
		public List<Artefact>	artefacts = new List<Artefact>();
		public Location			location = new Location();

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
		public Artefact	artefact;
		public Location	location = new Location();

		public	OnBoardItem(Artefact	artefact)
		{
			this.artefact = artefact;
		}
	}

	[Serializable]
	public class Board
	{
		public List<Player>			players = new List<Player>();
		public List<Artefact>		artefacts = new List<Artefact>();
		public List<OnBoardItem>	boardItems = new List<OnBoardItem>();

		public	Board(int playersCount, int artefactsCount)
		{
			this.players = new List<Player>(playersCount);

			for (int i = 0; i < playersCount; i++)
				this.players.Add(new Player());

			this.artefacts = new List<Artefact>(artefactsCount);
			this.boardItems = new List<OnBoardItem>(artefactsCount);

			for (int i = 0; i < artefactsCount; i++)
			{
				Artefact	artefact = new Artefact();
				this.artefacts.Add(artefact);
				this.boardItems.Add(new OnBoardItem(artefact));
			}
		}
	}

	public enum GamePhase
	{
		None = -1,
		FirstIdle,
		PlayersTurn,
		SecondIdle,
		GodTurn
	}

	public class GameManager : MonoBehaviour
	{
		public int	playersCount;
		public int	artefactsCount;

		public UnityEvent	FirstIdleEvent;
		public UnityEvent	PlayersTurnEvent;
		public UnityEvent	SecondIdleEvent;
		public UnityEvent	GodTurnEvent;

		[SerializeField]
		private GamePhase	gamePhase = GamePhase.None;
		[SerializeField]
		private Board		board;

		public void	StartNewParty(int playersCount, int artefactsCount)
		{
			this.gamePhase = GamePhase.None;
			this.board = new Board(playersCount, artefactsCount);
			this.NextPhase();
		}

		public void	NextPhase()
		{
			this.gamePhase = (GamePhase)(((int)this.gamePhase + 1) & 1);

			switch (this.gamePhase)
			{
				case GamePhase.FirstIdle:
					this.FirstIdleEvent.Invoke();
					break;
				case GamePhase.PlayersTurn:
					this.PlayersTurnEvent.Invoke();
					break;
				case GamePhase.SecondIdle:
					this.SecondIdleEvent.Invoke();
					break;
				case GamePhase.GodTurn:
					this.GodTurnEvent.Invoke();
					break;
				default:
					break;
			}
		}

		public void	GenerateTurnOrders(List<int> players)
		{
			players.Clear();

			for (int i = 0; i < this.Player.length; i++)
			{

			}
		}
	}
}