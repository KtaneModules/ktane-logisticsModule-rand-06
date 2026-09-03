using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class logisticsScript : MonoBehaviour {

	class Node
	{
		public int id;
		public Node(int num) { id = num; }
	}

	class Connection
	{
		public Node start, end;
		public int capacity, cost;

		public Connection(Node start, Node end, int capacity, int cost)
		{
			this.start = start;
			this.end = end;
			this.capacity = capacity;
			this.cost = cost;
		}
	}

	class Graph
	{
		public List<Node> nodes;
		public List<Connection> connections;

		public Graph(List<Node> nodes, List<Connection> connections)
		{
			this.nodes = nodes;
			this.connections = connections;
		}
		public Graph()
		{
			nodes = new List<Node>();
			connections = new List<Connection>();
		}

		private static float getWeightForRandom(int num, int den)
		{
			if (den == 0 || num == 0 || num>=den) return 0f;
			float x = (float)(num - 1) / (den - 1);
			return 1f - Mathf.Sqrt(x*(2-x));
		}

		private static int getWeightedRandom(int max)
		{
			float x = UnityEngine.Random.value;
			float w = Mathf.Sign(.5f - x) * (Mathf.Sqrt(x*(1-x)) - .5f) + .5f;
			return (int)(max * w) + 1;
		}
		
		public Graph(int amountOfNodes, int maxCapacity, int maxCost)
		{
			nodes = Enumerable.Range(0,amountOfNodes).Select(x => new Node(x)).ToList();
			List<Connection> list = new List<Connection>();
			for (int i = 0; i < amountOfNodes; i++)
			{
				for (int j = i + 1; j < amountOfNodes; j++)
				{
					if (UnityEngine.Random.value < getWeightForRandom(j - i, amountOfNodes - i))
					{
						list.Add(new Connection(nodes[i], nodes[j], getWeightedRandom(maxCapacity), getWeightedRandom(maxCost)));
					}
				}
			}
			connections = list;
		}

		struct NodeWeight
		{
			public int weight;
			public List<Node> path;

			public NodeWeight(int weight, List<Node> path)
			{
				this.weight = weight;
				this.path = path;
			}
		}

		public List<Node> getCheapestPath()
		{
			Dictionary<Node, NodeWeight> dict = nodes
				.Select((x,i)=> i==0? 
					new KeyValuePair<Node, NodeWeight>(
						x, new NodeWeight(0, new List<Node>{x})
					):
					new KeyValuePair<Node, NodeWeight>(
						x, new NodeWeight(-1, new List<Node>())
						)).ToDictionary(x=>x.Key, x=>x.Value);
			foreach (KeyValuePair<Node, NodeWeight> mark in dict.Take(dict.Count - 1))
			{
				foreach (var connection in connections.Where(x => x.start == mark.Key))
				{
					if (dict[connection.end].weight == -1 || 
					    dict[connection.start].weight + connection.cost < dict[connection.end].weight
					    )
					{
						dict[connection.end] = new NodeWeight(dict[connection.start].weight + connection.cost,
							dict[connection.start].path.Append(connection.end).ToList());
					}
				}
			}
			return dict[nodes.Last()].path.Contains(nodes.First()) ? new List<Node>() : dict[nodes.Last()].path;
		}
	}
	
	
	
	
	public List<KMSelectable> numberedButtons;
	public KMSelectable modeButton, deleteButton, upButton, downButton;
	public TextMesh listMesh, bottomMesh;

	private bool submitMode;
	private int submittedAns;
	private int nodesAmount = 10;
	private int midInput;
	private int currentPage, currentScreen;
	private int phiMax, phiExpected;
	private int expectedAnswer;


	int getAmountOfPages(int index)
	{
		return 1;
	}

	string getRenderString(int index, int page)
	{
		return "";
	}
	void renderScreen(int index, int page = 0)
	{
		currentScreen = index;
		currentPage = page;
		switch (index)
		{
			case 0:
				listMesh.text = $"Input any\nnode number\nto start.\n\n|X| = {nodesAmount}\nφmax = {phiMax}";
				bottomMesh.text = "\nRead mode:0";
				break;
			case -1:
				listMesh.text = $"Σφi = {phiExpected}\n\nΣdi = {submittedAns}";
				bottomMesh.text = "Input mode.";
				break;
			default:
				listMesh.text = getRenderString(currentScreen, currentPage);
				bottomMesh.text = (getAmountOfPages(currentPage) == 1 ? 
					                  "" : $"Page {currentPage + 1}/{getAmountOfPages(currentPage)}") +
				                  $"\nRead mode:{currentScreen}";
				break;
		}
	}
	void solve(){}

	void getValues()
	{
		
	}
	
	void Start () {
		for (int i = 0; i < numberedButtons.Count; i++)
		{
			int i1 = i;
			numberedButtons[i1].OnInteract += delegate
			{
				if (submitMode)
				{
					if (submittedAns < 100000) submittedAns = submittedAns *10 + i1;
					renderScreen(-1);
				}
				else
				{
					if (nodesAmount <= 10)
					{
						renderScreen(i1 > nodesAmount - 1 || i1 == 0?0:i1);
					}
					else
					{
						if (midInput == 0 && i1 == 0) renderScreen(0);
						else
						{
							if (midInput == 0)
							{
								if (i1*10 > nodesAmount) renderScreen(i1);
								else midInput = i1;
							}
							else
							{
								if (midInput*10 + i1 >= nodesAmount) renderScreen(0);
								else renderScreen(midInput*10 + i1);
								midInput = 0;
							}
						}
					}
				}
				return false;
			};
		}
		modeButton.OnInteract += delegate {
			
			if (submitMode)
			{
				if (submittedAns == 0)
				{
					renderScreen(0);
					submitMode = false;
				}
				else
				{
					if (submittedAns == expectedAnswer) solve();
					else GetComponent<KMBombModule>().HandleStrike();
				}
			}
			else
			{
				submitMode = true;
				renderScreen(-1);
			}
			return false;
		};
		deleteButton.OnInteract += delegate
		{
			if (submitMode)
			{
				submittedAns /= 10;
				renderScreen(-1);
			}
			else renderScreen(0);

			return false;
		};
		upButton.OnInteract += delegate
		{
			if (currentPage != 0) renderScreen(currentScreen, currentPage - 1);
			return false;
		};
		downButton.OnInteract += delegate
		{
			if (currentPage < getAmountOfPages(currentScreen) - 1) renderScreen(currentScreen, currentPage + 1);
			return false;
		};
		renderScreen(0);
	}
}
