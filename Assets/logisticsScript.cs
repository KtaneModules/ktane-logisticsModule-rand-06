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

		public static bool operator ==(Node a, Node b) => a.id == b.id;
		public static bool operator !=(Node a, Node b) => !(a==b);
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
				for (int j = i + 1; j < amountOfNodes; j++)
					if (UnityEngine.Random.value < getWeightForRandom(j - i, amountOfNodes - i))
						list.Add(new Connection(nodes[i], nodes[j], getWeightedRandom(maxCapacity), getWeightedRandom(maxCost)));
			connections = list;
		}

		struct NodeWeight { public int weight; public List<Node> path; public NodeWeight(int weight, List<Node> path) { this.weight = weight; this.path = path; } }

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
				foreach (var connection in connections.Where(x => x.start == mark.Key))
					if (dict[connection.end].weight == -1 || dict[connection.start].weight + connection.cost < dict[connection.end].weight)
						dict[connection.end] = new NodeWeight(dict[connection.start].weight + connection.cost,
							dict[connection.start].path.Append(connection.end).ToList());
			return dict[nodes.Last()].path.Contains(nodes.First()) ? new List<Node>() : dict[nodes.Last()].path;
		}

		public int getPassthroughValue(List<Node> path){
			int ans = -1;
			for (int i=0; i<path.Count() - 1; i++)
				if (ans == -1 || ans > connections.First(x => x.start == path[i] && x.end == path[i+1]).capacity) ans = connections.First(x => x.start == path[i] && x.end == path[i+1]).capacity;
			return ans;
		}

		public int getCost(List<Node> path){
			int ans = 0;
			for (int i=0; i<path.Count() - 1; i++)
				ans += connections.First(x => x.start == path[i] && x.end == path[i+1]).cost;
			return ans;
		}

		public void subtractPath(List<Node> path, int passthrough){
			for (int i=0; i<path.Count() - 1; i++)
				connections.First(x => x.start == path[i] && x.end == path[i+1]).capacity -= passthrough;
			if (connections.Any(x=>x.capacity<0)) Debug.LogError("Path exception: found connection with negative capacity");
			connections = connections.Where(x=> x.capacity>0);
		}

		//me when paranoia
		public Graph deepCopy(this Graph graph) => new Graph(graph.nodes.Select(x => new Node(x.id)).ToList(), 
			graph.connections.Select(x => new Connection(new Node(x.start.id), new Node(x.end.id), x.capacity, x.cost)).ToList());
	
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

	private Graph graph;
	private int amountOfNodes = 10, maxCapacity = 20, maxCost = 35;
	private const int connectionsForPage = 9;
	private bool moduleSolved;


	int getAmountOfPages(int index) => (graph.connections.Where(x => x.start.id == index).Count() - 1) / connectionsForPage + 1;
	string getRenderString(int index, int page) => graph.connections.Where(x => x.start.id == index).Skip((index - 1) * connectionsForPage).Take(connectionsForPage)
			.Select( x => String.Format("{0}>{1}:{2}@{3}", x.start.id, x.end.id, x.capacity, x.cost).Aggregate((a,b) => a + "\n" + b);
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
				bottomMesh.text = (getAmountOfPages(currentPage) == 1 ? "" : $"Page {currentPage + 1}/{getAmountOfPages(currentPage)}") + $"\nRead mode:{currentScreen}";
				break;
		}
	}
	void solve(){

		GetComponent<KMBombModule>().HandlePass();
		bottomMesh.text = "Solved!";
		moduleSolved = true;
	}

	void getValues()
	{
		// To find phi-max, get cheapest paths until path is empty, sum up all of passthroughs
		Graph copy = graph.deepCopy();
		List<Node> currentPath = copy.getCheapestPath();
		while (!currentPath.IsEmpty()){
			int passthrough = copy.getPassthroughValue(currentPath);
			phiMax += passthrough;
			copy.subtractPath(currentPath, passthrough);
			currentPath = copy.getCheapestPath();
		}

		// Expected-phi will be (.5 + LSN / 36) * phi-max
		phiExpected = (int)(phiMax * (.5m + GetComponent<KMBombInfo>().GetSerialNumberDigits().Last() / 36m));

		// Expected answer... well, you'll see
		int remainingPhi = phiExpected;
		copy = graph.deepCopy();
		while(remainingPhi > 0){
			List<Node> currentPath = copy.getCheapestPath();
			int passthrough = copy.getPassthroughValue(currentPath);
			if (passthrough > remainingPhi) passthrough = remainingPhi;
			expectedAnswer += passthrough * copy.getCost(currentPath);
			remainingPhi -= passthrough;
			copy = copy.subtractPath(currentPath, passthrough);
		}
	}
	
	void Start () {
		graph = new Graph(amountOfNodes, maxCapacity, maxCost);
		getValues();
		for (int i = 0; i < numberedButtons.Count; i++)
		{
			int i1 = i;
			numberedButtons[i1].OnInteract += delegate
			{
				if (moduleSolved) return false;
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
							if (midInput == 0) {
								if (i1*10 > nodesAmount) renderScreen(i1);
								else midInput = i1;
							} else {
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
			if (moduleSolved) return false;
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
			if (moduleSolved) return false;
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
			if (moduleSolved) return false;
			if (currentPage != 0) renderScreen(currentScreen, currentPage - 1);
			return false;
		};
		downButton.OnInteract += delegate
		{
			if (moduleSolved) return false;
			if (currentPage < getAmountOfPages(currentScreen) - 1) renderScreen(currentScreen, currentPage + 1);
			return false;
		};
		renderScreen(0);
	}
}
