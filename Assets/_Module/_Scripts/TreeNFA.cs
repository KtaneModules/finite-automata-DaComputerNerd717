using System.Collections.Generic;
using System.Linq;
using System.Text;
//using UnityEngine;

namespace DCN.FiniteAutomata{
	public class TreeNFA {
		List<Node> nodes;
		List<Edge> edges;
		public bool isValid;

		public TreeNFA(TreeNFA tree) : this(){
			//copy lists and validity
			isValid = tree.isValid;
			for (int i = 0; i < tree.nodes.Count; i++)
				AddNode(tree.nodes[i].start, tree.nodes[i].goal);
			for(int i = 0;i < tree.edges.Count; i++)
				AddEdge(tree.edges[i].fromDex, tree.edges[i].toDex, tree.edges[i].tree.Clone());
		}

		public TreeNFA(){
			nodes = new List<Node>();
			edges = new List<Edge>();
			isValid = true;
		}

		public TreeNFA(bool valid) : this(){
			isValid = valid;
		}

		public TreeNFA(OpTree tree) : this()
		{
			AddEdge(AddNode(true, false), AddNode(false, true), tree);
			isValid = true;
        }

		public int FindNode(Node n){
			for(int i = 0; i < nodes.Count; i++){
				if(n == nodes[i]){
					return i;
				}
			}
			return -1;
		}

		public int AddEdge(int from, int to, OpTree tree){
			Edge e = new Edge();
			e.from = nodes[from];
			e.to = nodes[to];
			e.fromDex = from;
			e.toDex = to;
			e.tree = tree;
			return AddEdge(e);
		}

		public int AddEdge(Node from, Node to, OpTree tree){
			Edge e = new Edge();
			e.from = from;
			e.to = to;
			e.fromDex = FindNode(from);
			e.toDex = FindNode(to);
			e.tree = tree;
			return AddEdge(e);
		}

		public int AddEdge(Edge e){
			if (e.tree is EmptyTree)
				return -1;
			e.to.inEdges.Add(e);
			e.from.outEdges.Add(e);
			edges.Add(e);
			return edges.Count - 1;
		}

		public int AddNode(Node n){
			nodes.Add(n);
			return nodes.Count - 1;
		}

		public int AddNode(){
			return AddNode(new Node());
		}

		public int AddNode(bool isStart, bool isGoal){
			Node n = new Node();
			n.goal = isGoal;
			n.start = isStart;
			return AddNode(n);
		}

		public void RemoveNode(int dex){
			RemoveNode(nodes[dex]);
		}

		public void RemoveNode(Node n){
            for (int i = 0; i < n.outEdges.Count; i++){
				RemoveEdge(n.outEdges[i]);
			}
			for(int i = 0; i < n.inEdges.Count; i++){
				RemoveEdge(n.inEdges[i]);
			}
            nodes.Remove(n);
			//Update indices now that the node's removed
			UpdateIndices(false);
		}

		public void RemoveEdge(Edge e){
			bool found = edges.Remove(e);
			found |= e.to.inEdges.Remove(e);
			found |= e.from.outEdges.Remove(e);
			if(found)
				UpdateIndices(false);
		}

		public void RemoveEdge(int dex){
			edges[dex].to.inEdges.Remove(edges[dex]);
			edges[dex].from.outEdges.Remove(edges[dex]);
			edges.RemoveAt(dex);
			UpdateIndices(false);
		}

		private void UpdateIndices(bool notify = true)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				foreach(Edge e in nodes[i].outEdges)
				{
					if (notify && e.fromDex != i)
						UnityEngine.Debug.LogError($"Node {i} is connected to node {e.toDex} on {e.tree} but this edge has fromDex={e.fromDex}");
					e.fromDex = i;
					if (notify && e.from != nodes[i])
						UnityEngine.Debug.LogError($"Node {i} is connected to node {e.toDex} on {e.tree} but this edge has the wrong from");
					e.from = nodes[i];
				}
				foreach(Edge e in nodes[i].inEdges)
				{
					if (notify && e.toDex != i)
						UnityEngine.Debug.LogError($"Node {i} is connected from {e.fromDex} on {e.tree} but this edge has toDex={e.toDex}");
					e.toDex = i;
					if(notify && e.to != nodes[i])
						UnityEngine.Debug.LogError($"Node {i} is connected from {e.fromDex} on {e.tree} but this edge has the wrong to");
					e.to = nodes[i];
				}
			}
		}

		public static TreeNFA DoComplement(TreeNFA tree){
			bool goal = tree.nodes[0].goal;
			TreeNFA newTree = new TreeNFA(tree);
			foreach(Node n in newTree.nodes){
				n.goal = !n.goal;
			}
			UnityEngine.Debug.Assert(tree.nodes[0].goal == goal);
			return newTree;
		}

		public int GetNodeCount()
		{
			return nodes.Count; 
		}

		public int GetEdgeCount() {
			return edges.Count; 
		}
		
		public TreeNFA DoComplement(){
			return DoComplement(this);
		}

		/// <summary>
		/// Both inputs must be DFAs
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>The intersection of a and b</returns>
		public static TreeNFA DoIntersection(TreeNFA a, TreeNFA b){
			if(!a.isValid || !b.isValid){
				return new TreeNFA(false);
			}
            //UnityEngine.Debug.Log("Intersect");
            return SetOpHelper(a, b, (x, y) => x && y);
		}

		/// <summary>
		/// Both inputs must be DFAs
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>The union of a and b</returns>
		public static TreeNFA DoUnion(TreeNFA a, TreeNFA b){
			if(!a.isValid || !b.isValid){
				return new TreeNFA(false);
			}
			//UnityEngine.Debug.Log("Union");
			return SetOpHelper(a, b, (x, y) => x || y);
		}

		//static uint iterationCount = 0;

		public delegate bool BinOp(bool a, bool b);
		
		//TODO verify that this works right
		static TreeNFA SetOpHelper(TreeNFA a, TreeNFA b, BinOp isGoal){
			//UnityEngine.Debug.Log("DFA a\n"+a.ToString());
			//UnityEngine.Debug.Log("DFA b\n"+b.ToString());
			int[,] groupNodes = new int[a.nodes.Count, b.nodes.Count];
			for(int i = 0; i < a.nodes.Count; i++){
				for(int j = 0; j < b.nodes.Count; j++){
					groupNodes[i,j] = -1; //default value
				}
			}
            a.UpdateIndices();
            b.UpdateIndices();
            int startDexA = -1;
			for(int i = 0; i < a.nodes.Count; i++)
			{
				if (a.nodes[i].start) startDexA = i;
			}
			int startDexB = -1;
			for(int i = 0; i < b.nodes.Count; i++)
			{
				if (b.nodes[i].start) startDexB = i;
			}
			if(startDexA == -1)
			{
				UnityEngine.Debug.LogError("First argument missing a start state");
				return null;
			}
			if (startDexB == -1)
			{
				UnityEngine.Debug.LogError("Second argument missing a start state");
				return null;
			}
			//UnityEngine.Debug.Log(a.ToString());
			//UnityEngine.Debug.Log(b.ToString());
			//UnityEngine.Debug.Log($"start: ({startDexA}, {startDexB}). Size: ({a.nodes.Count}, {b.nodes.Count})");
			TreeNFA output = new TreeNFA();
			int startDex = output.AddNode(true, isGoal(a.nodes[startDexA].goal, b.nodes[startDexB].goal));
			groupNodes[startDexA, startDexB] = startDex; 
			Queue<int[]> frontier = new Queue<int[]>();
			frontier.Enqueue(new int[]{startDexA, startDexB});
			while(frontier.Count > 0){
				//if(iterationCount++ > 100000)
				//{
				//	UnityEngine.Debug.LogError("Infinite Loop");
				//	return null;
				//}
				int[] dexes = frontier.Dequeue();
				//Find the destinations for each graph for each input
				//Naming scheme: graphByInput
				int aByA = -1, aByB = -1, bByA = -1, bByB = -1; //All will be set 
				for(int i = 0; i < 2; i++){ //Since it's a DFA, we know all nodes have length 2
					Node aNode = a.nodes[dexes[0]];
					if (aNode.outEdges.Count != 2)
						UnityEngine.Debug.LogError("A node is missing outgoing edges?\n" + a.ToString());
					Edge eA = aNode.outEdges[i];
					if(eA.tree is A){
						aByA = eA.toDex;
					}else{
						aByB = eA.toDex;
					}
					Node bNode = b.nodes[dexes[1]];
                    if (bNode.outEdges.Count != 2)
                        UnityEngine.Debug.LogError("A node is missing outgoing edges?\n" + b.ToString());
                    Edge eB = bNode.outEdges[i];
					if(eB.tree is A){
						bByA = eB.toDex;
					}else{
						bByB = eB.toDex;
					}
				}
				//Handle the a input ones
				int oldDex = groupNodes[dexes[0], dexes[1]];
				if (aByA >= a.nodes.Count || aByA < 0)
					UnityEngine.Debug.LogError($"aByA = {aByA}/{a.nodes.Count}");
				if (aByB >= a.nodes.Count || aByB < 0)
					UnityEngine.Debug.LogError($"aByB = {aByB}/{a.nodes.Count}");
				int newDex = groupNodes[aByA, bByA];
				if(newDex == -1){
					newDex = output.AddNode(false, isGoal(a.nodes[aByA].goal, b.nodes[bByA].goal));
					groupNodes[aByA, bByA] = newDex;
					frontier.Enqueue(new int[]{aByA, bByA});
					//UnityEngine.Debug.Log($"Creating new node {newDex} with a edge at ({aByA}, {bByA})");
				}
				UnityEngine.Debug.Assert(oldDex >= 0);
				output.AddEdge(oldDex, newDex, A.INSTANCE);
                //Now handle b
                if (bByA >= b.nodes.Count || bByA < 0)
                    UnityEngine.Debug.LogError($"bByA = {bByA}/{b.nodes.Count}");
                if (bByB >= b.nodes.Count || bByB < 0)
                    UnityEngine.Debug.LogError($"bByB = {bByB}/{b.nodes.Count}");
                newDex = groupNodes[aByB, bByB];
				if(newDex == -1){
					newDex = output.AddNode(false, isGoal(a.nodes[aByB].goal, b.nodes[bByB].goal));
					groupNodes[aByB, bByB] = newDex;
					frontier.Enqueue(new int[]{aByB, bByB});
                    //UnityEngine.Debug.Log($"Creating new node {newDex} with b edge at ({aByB}, {bByB})");
                }
				output.AddEdge(oldDex, newDex, B.INSTANCE);
				//UnityEngine.Debug.Log("Current graph");
				//UnityEngine.Debug.Log(output.ToString());
			}
			//UnityEngine.Debug.Log(output.ToString());
			return output;
		}

		public TreeNFA DoIntersection(TreeNFA other){
			return DoIntersection(this, other);
		}

		public TreeNFA DoUnion(TreeNFA other) {
			return DoUnion(this, other);
		}

		//public void TrimDisconnected()
		//{
		//	List<Node> toRemove = new List<Node>();
		//	foreach (Node node in nodes)
		//	{
		//              //if (iterationCount++ > 100000)
		//              //{
		//              //    UnityEngine.Debug.LogError("Infinite Loop");
		//              //    return;
		//              //}
		//              if (node.inEdges.All(edge => node.outEdges.Contains(edge)) && !node.start && !node.goal)
		//		{
		//			toRemove.Add(node);
		//		}
		//	}
		//	foreach(Node node in toRemove)
		//	{
		//              //if (iterationCount++ > 100000)
		//              //{
		//              //    UnityEngine.Debug.LogError("Infinite Loop");
		//              //    return;
		//              //}
		//              RemoveNode(node);
		//	}
		//}

		public void TrimDisconnected()
		{
			if (nodes.Count == 0)
				return;
			var query = nodes.Where(n => n.start);
			if (query.Count() != 1) //no start means all nodes are unreachable, and will be detected as incorrect in final equality check
				return;
            int start = FindNode(query.First());
			Queue<int> frontier = new Queue<int>();
			frontier.Enqueue(start);
            HashSet<int> connected = new HashSet<int>();
            do
			{
				int dex = frontier.Dequeue();
                connected.Add(dex);
				foreach (Edge e in nodes[dex].outEdges)
				{
					if (!connected.Contains(e.toDex))
					{
						frontier.Enqueue(e.toDex);
					}
				}
			} while (frontier.Count > 0);
			List<Node> toRemove = new List<Node>();
			for (int i = 0; i < connected.Count; i++)
			{
				if(!connected.Contains(i))
					toRemove.Add(nodes[i]);
			}
			foreach(Node n in toRemove)
			{
				RemoveNode(n);
			}
		}

		public Node GetNode(int dex)
		{
			return this.nodes[dex];
		}

		public bool HasStart()
		{
			return this.nodes.Where(n => n.start).Any();
        }

        public bool IsEqual(TreeNFA other, out string cause)
        {
			if(isValid != other.isValid){ //either invalid
				cause = "One of the NFAs was invalid";
                return false;
			}
			if(this.nodes.Where(n => n.start).Count() != 1) //this has no start, or too many
			{
				//UnityEngine.Debug.Log(ToString());
				cause = "Submitted NFA must have exactly 1 start";
				return false;
			}
            if (other.nodes.Where(n => n.start).Count() != 1) //other has no start, or too many
            {
				//UnityEngine.Debug.Log(other.ToString());
				cause = "Correct NFA does not have exactly 1 start. This is an error.";
                return false;
            }
            if (!isValid){ //both must be equal so this means both are invalid
				cause = "Both NFAs are invalid";
				return true;
			}
			TreeNFA diff1 = this.DoComplement().DoIntersection(other);
			//Debug.Log("correct - input:\n" + diff1.ToString());
			TreeNFA diff2 = other.DoComplement().DoIntersection(this);
			//Debug.Log("input - correct:\n" + diff2.ToString()); 
			UnityEngine.Debug.Assert(diff1.isValid);
			UnityEngine.Debug.Assert(diff2.isValid);
            if (diff1.nodes.Any(n => n.goal)){
				cause = "Submitted misses correct states: " + diff1.ToString();
				return false; //other is a superset of this, or neither is a subset of the other
			}
			if(diff2.nodes.Any(n => n.goal)){
				cause = "Submitted matches incorrect states: " + diff2.ToString();
				return false; //this is a superset of other
			}
			cause = "NFAs are equal";
			return true;
		}

		public override string ToString()
		{
			//var oldCount = iterationCount;
			//iterationCount = 0;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < this.nodes.Count; i++)
			{
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Infinite loop");
                //    return null;
                //}
                if (nodes[i].start)
					sb.Append(">");
				if (nodes[i].goal)
					sb.Append("+");
				sb.Append(i);
				sb.Append("\t");
				bool first = true;
				foreach (var edge in nodes[i].outEdges)
				{
                    //if (iterationCount++ > 100000)
                    //{
                    //    UnityEngine.Debug.LogError("Infinite loop");
                    //    return null;
                    //}
                    if (first)
						first = false;
					else 
						sb.Append(" ");
                    sb.Append(edge.tree.ToFormalRegexString());
					sb.Append(":");
                    sb.Append(edge.toDex);
				}
				sb.AppendLine();
            }
			//iterationCount = oldCount;
			return sb.ToString();
		}

		public void RemoveDisconnectedNodes()
		{
			int startState = nodes.FindIndex(node => node.start);
            if (startState == -1) //all states are disconnected if there is no start state
            {
				nodes.Clear();
				edges.Clear();
				AddNode(true, false);
				return;
            }
			reachedEdges.Clear();
			reachedNodes.Clear();
			couldReachGoal.Clear();
			FindNodesReachingGoal(startState);
			var toRemove = new List<int>();
			//Nodes we reached, but couldn't get to a goal from
			toRemove.AddRange(couldReachGoal.Where(kv => !kv.Value).Select(kv => kv.Key));
			//Nodes we couldn't even reach
			toRemove.AddRange(Enumerable.Range(0, nodes.Count).Where(x => !reachedNodes.Contains(x)));
			if (toRemove.Count == 0)
				return;
			int bh = AddNode(false, false);
			for(int i = 0; i < edges.Count; i++)
			{
				if (toRemove.Contains(edges[i].toDex))
				{
					SetEdgeTo(edges[i], bh);
				}
			}
			foreach (var node in toRemove)
				RemoveNode(node);
			//UnityEngine.Debug.Log(ToString());
        }

		private List<Edge> reachedEdges = new List<Edge>();
		private List<int> reachedNodes = new List<int>();
		private Dictionary<int, bool> couldReachGoal = new Dictionary<int, bool>();
		private bool FindNodesReachingGoal(int nodeDex)
		{
			bool found = false;
			reachedNodes.Add(nodeDex);
			couldReachGoal[nodeDex] = false;
			if (nodes[nodeDex].goal)
			{
                couldReachGoal[nodeDex] = true;
                found = true;
            }
			foreach (Edge e in nodes[nodeDex].outEdges)
			{
				if (reachedEdges.Contains(e)) //already tried this edge, going in circles
					continue;
				reachedEdges.Add(e);
				if(e.toDex == e.fromDex)
					continue;
				if(couldReachGoal.ContainsKey(e.toDex) && couldReachGoal[e.toDex]) //this is a cycle back, we found that this node already works
				{
					couldReachGoal[nodeDex] = true;
					found = true;
				}
                if (FindNodesReachingGoal(e.toDex))
                {
                    couldReachGoal[nodeDex] = true;
                    found = true;
                }
			}
			return found;
		}

		public OpTree ToOpTree()
		{
			//How to convert to regex
			//Convert cycles to *
				//this changes the number of outbound edges on the current node, but exclusively by reducing it
			//Convert chains to concat
				//If the current node is the middle, this exclusively reduces the amount of edges.
			//Convert parallels to union
				//Exclusively reduces edge count
			//Repeat until there's only one node (guaranteed?)
				//If we have a cycle in the graph at all, the concat rule will shrink the cycle
			//iterationCount = 0;
			//UnityEngine.Debug.Log(this.ToString());
			if(!isValid){
				return null;
			}
			if(nodes.Count() == 0){
				return EmptyTree.INSTANCE;
			}
			if(edges.Count() == 0){
				if(nodes.Any(n => n.start && n.goal)){
					return Epsilon.INSTANCE;
				}else{
					return EmptyTree.INSTANCE;
				}
			}
			if (nodes.All(node => node.goal))
				return Star.Of(Union.Of(A.INSTANCE, B.INSTANCE));
			if (!nodes.Any(node => node.goal))
				return EmptyTree.INSTANCE;
            //juust to be sure
            UpdateIndices();
			bool changed;
			do
			{
				//if(iterationCount++ > 200000)
				//{
				//	UnityEngine.Debug.LogError("Infinite Loop");
				//	return null;
				//}
                changed = false;
                List<Node> nodesToRemove = new List<Node>();
                for (int i = 0; i < nodes.Count; i++)
				{
					if (nodes[i].outEdges.Count == 0 && !nodes[i].goal)
					{
						changed = true;
						nodesToRemove.Add(nodes[i]);
					}
				}
				foreach (Node n in nodesToRemove) RemoveNode(n);
				if (changed) continue;
                //Check for duplicate nodes at all stages
                
                for(int i = 0; i < nodes.Count; i++)
				{
					Node a = nodes[i];
					for(int j = i+1; j < nodes.Count; j++)
					{
						Node b = nodes[j];
						if (b.ShouldMerge(a))
						{
							while(a.inEdges.Count > 0)
							{
								//UnityEngine.Debug.Log(a.inEdges.Count);
								//if(iterationCount++ > 100000)
								//{
								//	UnityEngine.Debug.LogError("Infinite Loop");
								//	return null;
								//}
								Edge e = a.inEdges[a.inEdges.Count-1];
								SetEdgeTo(e, j); //move so it connects to b
							}
							//UnityEngine.Debug.Log("Loop exited");
							nodesToRemove.Add(a);
							changed = true;
							break;
						}
					}
				}
                foreach (Node n in nodesToRemove) RemoveNode(n);
                if (changed) continue;
                //Duplicate edges are handled by union
                //Must happen before loop resolution; If we have parallel loops R and S, that must resolve not to R*|S* but to (R|S)*
                //Resolve parallels. Do before chains since this reduces the edge creation
                List<Edge> edgesToRemove = new List<Edge>();
				foreach (Node n in nodes)
				{
					for (int i = 0; i < n.outEdges.Count; i++)
					{
						Edge a = n.outEdges[i];
						for (int j = i + 1; j < n.outEdges.Count; j++)
						{
							Edge b = n.outEdges[j];
							if (a.fromDex == b.fromDex && a.toDex == b.toDex)
							{
								b.tree = Union.Of(a.tree, b.tree);
								edgesToRemove.Add(a);
								changed = true;
								break; //don't want to continue comparing to a removed term; if there are 3, then it will be found when i is what j is now
							}
						}
					}
				}
				foreach (Edge e in edgesToRemove) RemoveEdge(e);
				if (changed) continue; //get another pass at branches before we move on to cycles, just to be sure we got em all
				//Clear self-loops
				foreach (Node n in nodes)
				{
					//Node n = nodes[i];
					OpTree selfEdges = EmptyTree.INSTANCE;
					if (n.outEdges.Any(edge => edge.fromDex == edge.toDex))
					{
						//int dest = AddNode(false, n.goal);
						foreach (Edge e in n.outEdges)
						{
                            //if (iterationCount++ > 100000)
                            //{
                            //    UnityEngine.Debug.LogError("Infinite Loop");
                            //    return null;
                            //}
                            //Edge e = n.outEdges[j];
							if (e.toDex == e.fromDex) //1-cycle R, so we add R* to selfEdges
							{
								selfEdges = Union.Of(selfEdges, Star.Of(e.tree));
								edgesToRemove.Add(e);
							}
						}
						changed = true;
					}
					//Now, we take the union of all self-edges' stars and concat to non-self-edges
					//Thus, 1	a:1 b:2 becomes 1	a*b:2
					foreach(Edge e in n.outEdges)
					{
						//Edge e = n.outEdges[j];
						if (e.toDex != e.fromDex)
						{
							e.tree = Concat.Of(selfEdges, e.tree);
						}
					}
				}
                foreach (Edge e in edgesToRemove) RemoveEdge(e);
                if (nodes.Any(node => node.outEdges.Any(edge => edge.toDex == edge.fromDex)))
				{
					UnityEngine.Debug.LogError($"Could not remove a 1-loop\n{ToString()}");
					return null;
				}
				if (changed) continue;
                //Now we should have no self loops, so other ops are safe
				
				//Resolve chains
				foreach(Node n in nodes)
				{
					//Node n = nodes[i];
					if (!n.start && n.inEdges.Count > 0 && n.outEdges.Count > 0)
					{
						for(int j = 0; j < n.inEdges.Count; j++)
						{
							Edge a = n.inEdges[j];
							for(int k = 0; k < n.outEdges.Count; k++)
							{
                                //if (iterationCount++ > 100000)
                                //{
                                //    UnityEngine.Debug.LogError("Infinite Loop");
                                //    return null;
                                //}
                                Edge b = n.outEdges[k];
								if(j == k) 
									continue;
								if (a.Equals(b)) //preventing issues with cycles
									continue;
								OpTree tree = Concat.Of(a.tree, b.tree);
								AddEdge(a.fromDex, b.toDex, tree); //won't be an infinite loop since this isn't changing n's in or out edges
								if(!edgesToRemove.Contains(b))
									edgesToRemove.Add(b);
								changed = true;
								if(!nodesToRemove.Contains(n))
                                    nodesToRemove.Add(n);
                            }
							if(!edgesToRemove.Contains(a))
								edgesToRemove.Add(a);
                        }
					}
				}
				foreach (Edge e in edgesToRemove) RemoveEdge(e);
				foreach (Node n in nodesToRemove) RemoveNode(n);
			} while (changed);
			if(edges.Count > 1)
			{
				UnityEngine.Debug.LogError($"Could not resolve to a single regex:\n{ToString()}");
				return null;
			}
			return edges[0].tree;
		}

        private void SetEdgeFrom(Edge e, int node)
		{
			e.from.outEdges.Remove(e);
			e.fromDex = node;
			e.from = nodes[node];
			e.from.outEdges.Add(e);
		}

		private void SetEdgeTo(Edge e, int node)
		{
			e.to.inEdges.Remove(e);
			e.toDex = node;
			e.to = nodes[node];
			e.to.inEdges.Add(e);
		}

		private void SetEdgeTo(Edge e, Node n)
		{
			e.to.inEdges.Remove(e);
			e.toDex = FindNode(n);
			e.to = n;
			e.to.inEdges.Add(e);
		}

		private bool TreeHasCycles(OpTree tree, List<OpTree> handled)
		{
			//if(iterationCount++ > 100000)
			//	return true;
			if (tree is A || tree is B || tree is Epsilon || tree is EmptyTree)
				return false;
			if (handled.Contains(tree)) //we've been to this node before
				return true;
			handled.Add(tree);
			foreach (OpTree child in tree.GetChildren())
			{
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Infinite Loop");
                //    return true;
                //}
                if (child.GetChildren().Count == 0)
					continue; //skip a, b, epsilon
				if (TreeHasCycles(child, new List<OpTree>(handled)))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Processes the graph until all edges have either a or b as the representative strings, then converts to a FA_Graph with only a or b edges.
		/// </summary>
		/// <returns>A copy of this graph, processed to be a DFA.</returns>
		public TreeNFA ToDFA()
		{
			if (!isValid)
			{
				UnityEngine.Debug.LogError("Invalid TG");
				return new TreeNFA(false);
			}
			TreeNFA nfa = new TreeNFA(this);
			//First, resolve the transition graph to a NFA
			//iterationCount = 0;
			for (int i = 0; i < nfa.edges.Count;) //no i++ so we resolve this edge until it is just a, b, or epsilon
			{
                UpdateIndices();
                //UnityEngine.Debug.Log(nfa.ToString());
				//if (iterationCount++ > 200000)
				//{
				//	UnityEngine.Debug.LogError("Loop was infinite");
				//	nfa.isValid = false;
				//	return nfa;
				//}
				int newState, newEdge;
				Edge e = nfa.edges[i];
				if(e.tree is Union) { 
					for(int j = 1; j < e.tree.GetChildren().Count; j++)
					{
						nfa.AddEdge(e.from, e.to, e.tree.GetChildren()[j]);
					}
					e.tree = e.tree.GetChildren()[0];
					nfa.edges[i] = e;
				}
				else if (e.tree is Concat)
				{
					int lastNode = e.fromDex;
					for (int j = 0; j < e.tree.GetChildren().Count-1; j++)
					{
						int nextNode = nfa.AddNode();
						nfa.AddEdge(lastNode, nextNode, e.tree.GetChildren()[j]);
						lastNode = nextNode;
						//Debug.Log($"Concat child {i}: {nfa}");
					}
					//Debug.Log(lastNode);
					nfa.SetEdgeFrom(e, lastNode);
					e.tree = e.tree.GetChildren().Last();
					nfa.edges[i] = e;
					//Debug.Log("End of concat: " + nfa.ToString());
				}
				else if (e.tree is Star)
				{
					newState = nfa.AddNode();
					newEdge = nfa.AddEdge(e.fromDex, newState, Epsilon.INSTANCE);
					nfa.AddEdge(newState, e.toDex, Epsilon.INSTANCE);
					nfa.SetEdgeTo(e, newState);
					nfa.SetEdgeFrom(e, newState);
					e.tree = ((Star)e.tree).child;
					nfa.edges[i] = e;
				}
				else
				{
					i++;
				}
			}
			nfa.UpdateIndices();
			//UnityEngine.Debug.Log("TG after resolving operations:");
			//UnityEngine.Debug.Log(nfa.ToString());
			//All edges are now a, b, or epsilon
			//epsilon is not handled yet, since it's easier to handle after
			//Now we do NFA to DFA
			//First, we need a single start state
			int startDex = nfa.AddNode(true, false);
			Node startState = nfa.nodes[startDex];
			foreach (Node node in nfa.nodes)
			{
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Infinite Loop");
                //    return null;
                //}
                if (node.start)
				{
					node.start = false;
					nfa.AddEdge(startState, node, Epsilon.INSTANCE);
				}
			}
			startState.start = true;
            //UnityEngine.Debug.Log("TG after adding extra start state:");
            //UnityEngine.Debug.Log(nfa.ToString());
            //Now we resolve epsilon
            List<Edge> toRemove = new List<Edge>();
            List<Node> nodesToRemove = new List<Node>();
			//Check this
			for (int i = 0; i < nfa.edges.Count; i++)
			{
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Infinite Loop");
                //    return null;
                //}
                if (nfa.edges[i].tree is Epsilon)
				{
					//We want to remove epsilon edges, but not yet. Doing so may mess with goal transferring
					if (nfa.edges[i].toDex == nfa.edges[i].fromDex)
					{
						toRemove.Add(nfa.edges[i]);
					}
				}
			}
			foreach(Edge e in toRemove)
				nfa.RemoveEdge(e);
			//Debug.Log("NFA after removing epsilon self-loops");
			//Debug.Log(nfa.ToString());
			//Debug.Log("Resolving epsilon");
            for (int i = 0; i < nfa.edges.Count; i++)
            {
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Infinite Loop");
                //    return null;
                //}
                if (nfa.edges[i].tree is Epsilon) {
					//UnityEngine.Debug.Log($"Epsilon Edge: {nfa.edges[i].fromDex}→{nfa.edges[i].toDex}");
                    //We want to remove epsilon edges, but not yet. Doing so may mess with goal transferring
                    toRemove.Add(nfa.edges[i]);
					if (nfa.edges[i].toDex == nfa.edges[i].fromDex)
					{
						//UnityEngine.Debug.Log("Self-edge, removing");
						continue;
					}
					Edge epsil = nfa.edges[i];
                    //Debug.Log($"NFA while resolving epsilon on edge {epsil.from}→{epsil.to} :\n{nfa}");
                    epsil.from.goal |= epsil.to.goal;
                    if (epsil.from.start && epsil.from.outEdges.Count == 1) //we can reduce the start
                    {
						//UnityEngine.Debug.Log("Connects start to single node, simplifying");
						epsil.to.start = true;
						nodesToRemove.Add(epsil.from);
						continue;
                    }
                    foreach (Edge e in epsil.to.outEdges)
					{
						//does the edge we want to add already exist?
						//The edge we want to find is in both e.to.inEdges and epsil.from.outEdges with epsilon
						//Essentially, an edge from epsil.from to epsil.to
						//We can detect this by searching e.to.inEdges for edges where fromDex == epsil.fromDex
						if (e.Equals(epsil)) {
							continue;
						}
						if (!nfa.edges.Any(edge => edge.fromDex == epsil.fromDex && edge.toDex == e.toDex && edge.tree.Equals(e.tree)))
							nfa.AddEdge(epsil.fromDex, e.toDex, e.tree);
					}
					foreach (Edge e in epsil.from.inEdges)
					{
						//This time, e leads into epsil, so our target edge goes from e.from to epsil.to
						if (e.Equals(epsil)) {
							continue;
						}
						if(!nfa.edges.Any(edge => edge.fromDex == e.fromDex && edge.toDex == epsil.toDex && edge.tree.Equals(e.tree)))
							nfa.AddEdge(e.fromDex, epsil.toDex, e.tree);
					}
				}
			}
			//Remove flagged values
			foreach (Edge edge in toRemove)
				nfa.RemoveEdge(edge);
			foreach (Node node in nodesToRemove)
				nfa.RemoveNode(node);
			//UnityEngine.Debug.Log("NFA after resolving epsilon:");
			//UnityEngine.Debug.Log(nfa.ToString());
			bool changed;
			//iterationCount = 0;
			do
			{
				changed = false;
				//if(iterationCount++ > 1000)
				//{
				//	UnityEngine.Debug.LogError("Infinite Loop Detected");
				//	return new TreeNFA(false);
				//}
                nfa.UpdateIndices();
                List<int> removedEdgeKeys = new List<int>();
				//Trim duplicate edges
				for (int i = 0; i < nfa.edges.Count; i++)
				{
					for (int j = i + 1; j < nfa.edges.Count; j++) //anything lower, we already checked when i was that value, or is the same edge
					{
						if (nfa.edges[i].Equals(nfa.edges[j]) && !removedEdgeKeys.Contains(j) && !removedEdgeKeys.Contains(i)) //can't double remove
						{
							toRemove.Add(nfa.edges[j]);
							changed = true;
							removedEdgeKeys.Add(i);
						}
					}
				}
                //Remove flagged values
                nfa.UpdateIndices();
                foreach (Edge edge in toRemove)
					nfa.RemoveEdge(edge);
				//UnityEngine.Debug.Log("NFA after removing duplicate edges:");
				//UnityEngine.Debug.Log(nfa.ToString());
				if (changed) continue;
				nfa.UpdateIndices();
				//iterationCount = 0;
				//Trim duplicate nodes; two nodes which have the same goal status and same outgoing edges are identical and can be merged
				for (int i = 0; i < nfa.nodes.Count; i++)
				{
                    //if (iterationCount++ > 100000)
                    //{
                    //    UnityEngine.Debug.LogError("Infinite Loop");
                    //    return null;
                    //}
                    if (nfa.nodes[i].inEdges.Count == 0 && !nfa.nodes[i].start)
					{
						nodesToRemove.Add(nfa.nodes[i]);
						changed = true;
						continue;
					}
					for (int j = i + 1; j < nfa.nodes.Count; j++) //anything lower, we already checked when i was that value, or is the same node
					{
						Node a = nfa.nodes[i];
						Node b = nfa.nodes[j];
						if (a.goal == b.goal && a.outEdges.SequenceEqual(b.outEdges))
						{
							while (b.inEdges.Count > 0)
							{
								//if (iterationCount++ > 10000)
								//{
								//	UnityEngine.Debug.LogError("Loop was infinite");
								//	nfa.isValid = false;
								//	return nfa;
								//}
								nfa.SetEdgeTo(b.inEdges[0], i); //removes, so this loop isn't infinite
							}
							nodesToRemove.Add(b);
							if (b.start)
								a.start = true;
							changed = true;
						}
					}
				}
                //Remove flagged values
                nfa.UpdateIndices();
                foreach (Node node in nodesToRemove)
					nfa.RemoveNode(node);
				//UnityEngine.Debug.Log("NFA after removing duplicate states:");
				//UnityEngine.Debug.Log(nfa.ToString());
				nfa.UpdateIndices();
			} while (changed);

			//iterationCount = 0;
			//Add any missing outgoing edges, black hole state, if needed
			int bhState = -1;
			for (int i = 0; i < nfa.nodes.Count; i++)
			{
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Infinite Loop");
                //    return null;
                //}
                bool foundA = false, foundB = false;
				foreach (Edge e in nfa.nodes[i].outEdges)
				{
					if (!foundA && e.tree is A)
						foundA = true;
					if (!foundB && e.tree is B)
						foundB = true;
				}
				if (bhState == -1 && (!foundA || !foundB))
				{ //no BH and need one
					bhState = nfa.AddNode();
					nfa.AddEdge(bhState, bhState, A.INSTANCE);
					nfa.AddEdge(bhState, bhState, B.INSTANCE);
				}
				if (!foundA && foundB)
					nfa.AddEdge(i, bhState, A.INSTANCE);
				else if (!foundB && foundA)
					nfa.AddEdge(i, bhState, B.INSTANCE);
				else if (!foundA && !foundB && !nfa.nodes[i].goal)
				{
					//Any state where both outputs lead to BH and isn't a goal is is equivalent to it
					//Thus, let's remove this state and move its edges to BH, assuming it isn't already the BH
					if (i == bhState)
						continue;
					while (nfa.nodes[i].inEdges.Count > 0)
					{
						//if (iterationCount++ > 10000)
						//{
						//	UnityEngine.Debug.LogError("Loop was infinite");
						//	nfa.isValid = false;
						//	return nfa;
						//}
						nfa.SetEdgeTo(nfa.nodes[i].inEdges[0], bhState);
					}
					if (nfa.nodes[i].start)
						nfa.nodes[bhState].start = true;
					nodesToRemove.Add(nfa.nodes[i]);
				}else if(!foundA && !foundB)
				{
                    nfa.AddEdge(i, bhState, A.INSTANCE);
                    nfa.AddEdge(i, bhState, B.INSTANCE);
                }
			}
            //UnityEngine.Debug.Log("NFA after connecting crashes to BH:");
            //UnityEngine.Debug.Log(nfa.ToString());
			nfa.UpdateIndices();
            //Remove flagged values
            foreach (Node node in nodesToRemove)
				nfa.RemoveNode(node);
			nfa.UpdateIndices();
			//Handle group states
			int stateCount = nfa.nodes.Count;

			Dictionary<int[], int> dfaKeys = new Dictionary<int[], int>(new IntArrayComparer());
			Queue<int[]> frontier = new Queue<int[]>();
			startDex = -1;
			var startQuery = nfa.nodes.Where(n => n.start);
			if (startQuery.Any())
				startDex = nfa.FindNode(startQuery.First());
			else
				UnityEngine.Debug.LogError("Couldn't find start node");
			TreeNFA dfa = new TreeNFA();
			int[] startKey = new int[] { startDex };
            frontier.Enqueue(startKey);
			dfaKeys[startKey] = dfa.AddNode(true, startState.goal);
            //iterationCount = 0;
			//UnityEngine.Debug.Log("Converting to DFA");
            while (frontier.Count > 0)
			{
                //if (iterationCount++ > 100000)
                //{
                //    UnityEngine.Debug.LogError("Loop was infinite");
                //    nfa.isValid = false;
                //    return nfa;
                //}
				int[] currKey = frontier.Dequeue();
				//UnityEngine.Debug.Log("Current key: " + currKey.Select(x => x.ToString()).Aggregate((x, y) => x + " " + y));
				
				SortedDictionary<int, int> aDexes = new SortedDictionary<int, int>(), bDexes = new SortedDictionary<int, int>();
				foreach (int dex in currKey)
				{
					foreach(Edge edge in nfa.nodes[dex].outEdges)
					{
						if (edge.tree is A && !aDexes.ContainsKey(edge.toDex))
							aDexes.Add(edge.toDex, edge.toDex);
						if (edge.tree is B && !bDexes.ContainsKey(edge.toDex)) 
							bDexes.Add(edge.toDex, edge.toDex);
					}
				}
				int[] aKey = aDexes.Values.ToArray();
				int[] bKey = bDexes.Values.ToArray();
				if (!dfaKeys.ContainsKey(aKey))
				{
					frontier.Enqueue(aKey);
                    bool isGoal = aKey.Any(node => nfa.nodes[node].goal);
					//UnityEngine.Debug.Log($"Links: {aKey.Select(x => x.ToString()).Aggregate((x, y) => $"{x} {y}")}, goal: {isGoal}\nNFA: {nfa.ToString()}");
                    int newDex = dfa.AddNode(false, isGoal);
                    dfaKeys[aKey] = newDex;
				}
				dfa.AddEdge(dfaKeys[currKey], dfaKeys[aKey], A.INSTANCE);

				if (!dfaKeys.ContainsKey(bKey))
				{
					frontier.Enqueue(bKey);
					bool isGoal = bKey.Any(node => nfa.nodes[node].goal);
                    //UnityEngine.Debug.Log($"Links: {bKey.Select(x => x.ToString()).Aggregate((x, y) => $"{x} {y}")}, goal: {isGoal}\nNFA: {nfa.ToString()}");
                    int newDex = dfa.AddNode(false, isGoal);
					dfaKeys[bKey]= newDex;
				}
                dfa.AddEdge(dfaKeys[currKey], dfaKeys[bKey], B.INSTANCE);
				//UnityEngine.Debug.Log(dfa.ToString());
            }
			//UnityEngine.Debug.Log(dfa.ToString());
            return dfa;
		}

        protected class IntArrayComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] x, int[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(int[] obj)
            {
                return obj.Sum();
            }
        }
		
		public class Node{
			public List<Edge> outEdges = new List<Edge>(), inEdges = new List<Edge>();
			public bool start = false, goal = false;
            public bool ShouldMerge(Node n)
			{
				return !start && !n.start && (goal == n.goal)
					&& !outEdges.Any(e => !n.outEdges.Contains(e)) 
					&& !n.outEdges.Any(e => !outEdges.Contains(e));
			}
		}

		public class Edge{
			public OpTree tree;
			public Node to, from;
			public int fromDex, toDex;

            public override bool Equals(object obj)
            {
				Edge other = obj as Edge;
				if(other == null) return false;
				return other.toDex == toDex && other.fromDex == fromDex && other.tree.Equals(tree);
            }
            public override int GetHashCode()
            {
                return from.GetHashCode() ^ to.GetHashCode();
            }
        }

		public class IntsComparer : EqualityComparer<int[]>{
			public override bool Equals(int[] a, int[] b){
				return a.SequenceEqual(b);
			}
			public override int GetHashCode(int[] arr){
				int hash = 0;
				foreach(int n in arr){
					hash ^= n;
				}
				return hash;
			}
		}
	}
}