using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Random = System.Random;

public class logisticsScript : MonoBehaviour {

	class Node
	{
		public int id;
		public Node(int num) { id = num; }

		public static bool operator ==(Node a, Node b) 
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return a.id == b.id;
		}
		public static bool operator !=(Node a, Node b) => !(a==b);
		
		public override bool Equals(object obj)
		{
			Node other = obj as Node;
			if (other == null)
				return false;
			return this.id == other.id;
		}
    
		public override int GetHashCode()
		{
			return id.GetHashCode();
		}
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

		private static float getWeightForRandom(int I, int X, int N)
		{
			//if (den == 0 || num == 0 || num==init) return 0f;
			float x = (float)(X-1-I)/(N-I);
			//return 1f-Mathf.Sqrt(x*(2f-x));
			return 1f - x;
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
					if (UnityEngine.Random.value < getWeightForRandom(i, j, amountOfNodes))
						list.Add(new Connection(nodes[i], nodes[j], getWeightedRandom(maxCapacity), getWeightedRandom(maxCost)));
			connections = list;
			Debug.Log(String.Format("Created graph of nodes: {0} and connections:\n{1}", nodes.Select(x => (x.id+1).ToString()).Aggregate((a,b)=>a+", "+b),
				connections.Select( x => $"{x.start.id+1}>{x.end.id+1}:{x.capacity}@{x.cost}").Aggregate((a,b) => a + "\n" + b)
			));
		}

		struct NodeWeight { public int weight; public List<Node> path; public NodeWeight(int weight, List<Node> path) { this.weight = weight; this.path = path; } }

		public List<Node> getCheapestPath()
		{
			Dictionary<Node, NodeWeight> dict = nodes
				.Select(x=> x.id==0? 
					new KeyValuePair<Node, NodeWeight>(
						x, new NodeWeight(0, new List<Node>{x})
					):
					new KeyValuePair<Node, NodeWeight>(
						x, new NodeWeight(-1, new List<Node>())
						)).ToDictionary(x=>x.Key, x=>x.Value);
			foreach (Node node in nodes.OrderBy(x => x.id))
			{
				if (dict[node].weight == -1) continue;
				foreach (var connection in connections.Where(x => x.start == node))
				{
					int newWeight = dict[node].weight + connection.cost;
					if (dict[connection.end].weight == -1 || newWeight < dict[connection.end].weight)
					{
						List<Node> newPath = new List<Node>(dict[node].path);
						newPath.Add(connection.end);
						dict[connection.end] = new NodeWeight(newWeight, newPath);
					}
				}
			}
			return dict[nodes.Last()].path.Count > 0 ? dict[nodes.Last()].path : new List<Node>();
		}

		public int getPassthroughValue(List<Node> path){
			int ans = -1;
			for (int i=0; i<path.Count - 1; i++)
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
			connections = connections.Where(x=> x.capacity>0).ToList();
		}

		//me when paranoia
		public static Graph deepCopy(Graph graph) => new Graph(graph.nodes.Select(x => new Node(x.id)).ToList(), 
			graph.connections.Select(x => new Connection(new Node(x.start.id), new Node(x.end.id), x.capacity, x.cost)).ToList());
	
	}
	
	public List<KMSelectable> numberedButtons;
	public KMSelectable modeButton, deleteButton, upButton, downButton;
	public TextMesh listMesh, bottomMesh;

	private bool submitMode;
	private int submittedAns;
	private int currentPage, currentScreen;
	private int phiMax, phiExpected;
	private int expectedAnswer;

	private Graph graph;
	private int nodesAmount = 25, maxCapacity = 15, maxCost = 15;
	private const int connectionsForPage = 9;
	private bool moduleSolved;

	private Dictionary<char, KMSelectable> buttonsTP;
	private bool TPShouldStop;


	int getAmountOfPages(int index) => (graph.connections.Count(x => x.start.id == index) - 1) / connectionsForPage + 1;
	string getRenderString(int index, int page) => graph.connections.Where(x => x.start.id == index).Skip((page - 1) * connectionsForPage).Take(connectionsForPage)
			.Select( x => $"{x.start.id + 1}>{x.end.id + 1}:{x.capacity}@{x.cost}").Aggregate((a,b) => a + "\n" + b);
	void renderScreen(int index, int page = 0)
	{
		index -= 1;
		currentScreen = index;
		currentPage = page;
		switch (index)
		{
			case -1:
				listMesh.text = $"Input any\nnode number\nto start.\n\n|X| = {nodesAmount}\nφmax = {phiMax}";
				bottomMesh.text = "\nRead mode:0";
				break;
			case -2:
				listMesh.text = $"Σφi = {phiExpected}\n\nΣdi = {submittedAns}";
				bottomMesh.text = "\nInput mode.";
				break;
			default:
				listMesh.text = getRenderString(currentScreen, currentPage);
				bottomMesh.text = (getAmountOfPages(currentPage) == 1 ? "" : $"Page {currentPage + 1}/{getAmountOfPages(currentPage)}") + $"\nRead mode:{currentScreen+1}";
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
		// To find phi-max, get the cheapest paths until path is empty, sum up all of passthroughs
		Graph copy = Graph.deepCopy(graph);
		List<Node> currentPath = graph.getCheapestPath();
		while (currentPath.Count>0){
			int passthrough = copy.getPassthroughValue(currentPath);
			phiMax += passthrough;
			copy.subtractPath(currentPath, passthrough);
			currentPath = copy.getCheapestPath();
		}

		// Expected-phi will be (.5 + LSN / 36) * phi-max
		phiExpected = (int)(phiMax * (.5m + GetComponent<KMBombInfo>().GetSerialNumberNumbers().Last() / 36m));

		// Expected answer... well, you'll see
		int remainingPhi = phiExpected;
		copy = Graph.deepCopy(graph);
		while(remainingPhi > 0){
			currentPath = copy.getCheapestPath();
			int passthrough = copy.getPassthroughValue(currentPath);
			if (passthrough > remainingPhi) passthrough = remainingPhi;
			expectedAnswer += passthrough * copy.getCost(currentPath);
			remainingPhi -= passthrough;
			copy.subtractPath(currentPath, passthrough);
		}
	}
	
	void Start ()
	{
		buttonsTP = new Dictionary<char, KMSelectable>
		{
			{ 'M', modeButton },
			{ '<', deleteButton},
			{'L',deleteButton},
			{'V',downButton},
			{'D',downButton},
			{'^',upButton},
			{'U',upButton},
			
			{'0',numberedButtons[0]},
			{'1',numberedButtons[1]},
			{'2',numberedButtons[2]},
			{'3',numberedButtons[3]},
			{'4',numberedButtons[4]},
			{'5',numberedButtons[5]},
			{'6',numberedButtons[6]},
			{'7',numberedButtons[7]},
			{'8', numberedButtons[8]},
			{'9',numberedButtons[9]}
		};
		
		graph = new Graph(nodesAmount, maxCapacity, maxCost);
		Debug.Log($"Initial cheapest path: {graph.getCheapestPath().Select(x=> (x.id+1).ToString()).Aggregate((a,b)=>a+" > "+b)}");
		getValues();
		Debug.Log($"Phi-max: {phiMax}, expected: {phiExpected}. Answer: {expectedAnswer}");
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
						int numberOnScreen = currentScreen + 1;
						if (numberOnScreen * 10 + i1 < nodesAmount) renderScreen(numberOnScreen * 10 + i1);
						else renderScreen(i1);
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
					else
					{
						GetComponent<KMBombModule>().HandleStrike();
						TPShouldStop = true;
					}
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
			else
			{
				renderScreen(0);
			}

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

	private string TwitchHelpMessage =
		@"Use !{0} digit to press digit, M to press M, </L to press <, ^/U to press Up and v/D to press Down.";

	
	
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToUpperInvariant();
		if (!Regex.Match(command, @"^[0-9MLVUD\^< ]+$").Success)
		{
			yield return "sendtochaterror Invalid command.";
			yield break;
		}

		foreach (char c in command)
		{
			if (c!=' ') buttonsTP[c].OnInteract();
			if (TPShouldStop)
			{
				TPShouldStop = false;
				yield break;
			}
			yield return new WaitForSeconds(0.1f);
		}
	}

}
