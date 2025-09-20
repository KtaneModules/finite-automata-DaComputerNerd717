using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using UnityEditor.Animations;

namespace DCN.FiniteAutomata
{
    public abstract class OpTree
    {
        public Regex cachedRegex = null;
        public abstract List<OpTree> GetChildren();
        public abstract string ToSystemRegex();
        //public abstract FormalRegex ToFormalRegex();
        public abstract string ToFormalRegexString();
        public abstract OpTree Clone();
        protected void MarkChanged()
        {
            cachedRegex = null;
        }
        public bool IsMatch(string str)
        {
            if (this is EmptyTree) 
                return str.Count() == 0;
            if(cachedRegex == null)
                cachedRegex = new Regex($"^{ToSystemRegex()}$");
            return cachedRegex.IsMatch(str);
        }

        public abstract bool IsMatch(OpTree tree);
        
        public abstract List<string> GetAllOptions();

        public void Optimize()
        {
            foreach(OpTree t in GetChildren())
                t.Optimize();
        }
        public abstract bool IsSameType(OpTree tree);
        //Distribution of concat:
        //a(a|b)(a|e)
        //(aa|ab)(a|e)
        //aaa|aba|aa|ab
        public bool IsEqual(OpTree other)
        {
            if (!IsSameType(other))
                return false;
            var children = GetChildren();
            var otherChildren = other.GetChildren();
            if (children.Count != otherChildren.Count)
                return false;
            for(int i = 0; i < children.Count; i++)
            {
                if (!children[i].IsEqual(otherChildren[i]))
                    return false;
            }
            return true;
        }

        public int GetOpCount(int count = 1) {
            var children = GetChildren();
            count += children.Count - children.Where(child => child is EmptyTree).Count();
            foreach(var child in GetChildren())
            {
                count = child.GetOpCount(count);
            }
            return count;
        }

        public abstract string ToString(int tabCount = 0);
        /*
        public static OpTree FromRegex(FormalRegex regex)
        {
            string str = regex.ToString();
            //tokenizing is barely needed, but we need to handle concat stuff
            for (int i = 1; i < str.Length; i++)
            {
                if (str[i] == 'a' || str[i] == 'b' || str[i] == 'ε' || str[i] == '(')
                    if (str[i - 1] == ')' || str[i - 1] == '*' || str[i - 1] == 'a' || str[i - 1] == 'b' || str[i - 1] == 'ε')
                        str = str.Insert(i, "+"); //+ will be the token for concat
            }
            //now we've got our string properly set up, the char stream is basically tokens now
            Stack<OpTree> outputs = new Stack<OpTree>();
            Stack<char> opStack = new Stack<char>();
            OpTree left, right;
            for (int i = 0;i < str.Length; i++)
            {
                switch (str[i])
                {
                    case 'a':
                        outputs.Push(A.INSTANCE);
                        break;
                    case 'b':
                        outputs.Push(B.INSTANCE);
                        break;
                    case 'ε':
                        outputs.Push(Epsilon.INSTANCE);
                        break;
                    case '*':
                        while (opStack.Count > 0 && opStack.Peek() == '*')
                        {
                            opStack.Pop();
                            outputs.Push(Star.Of(outputs.Pop()));
                        }
                        opStack.Push('*');
                        break;
                    case '+':
                        while(opStack.Count > 0 && (opStack.Peek() == '*' || opStack.Peek() == '+'))
                        {
                            switch (opStack.Pop())
                            {
                                case '*':
                                    outputs.Push(Star.Of(outputs.Pop()));
                                    break;
                                case '+':
                                    right = outputs.Pop();
                                    left = outputs.Pop();
                                    outputs.Push(Concat.Of(left, right));
                                    break;
                            }
                        }
                        opStack.Push('+');
                        break;
                    case '|':
                        while(opStack.Count > 0 && opStack.Peek() != '(') //only four things go on the opstack
                        {
                            switch (opStack.Pop())
                            {
                                case '*':
                                    outputs.Push(Star.Of(outputs.Pop()));
                                    break;
                                case '+':
                                    right = outputs.Pop();
                                    left = outputs.Pop();
                                    outputs.Push(Concat.Of(left, right));
                                    break;
                                case '|':
                                    right = outputs.Pop();
                                    left = outputs.Pop();
                                    outputs.Push(Union.Of(left, right));
                                    break;
                            }
                        }
                        break;
                    case '(':
                        opStack.Push('(');
                        break;
                    case ')':
                        while (opStack.Count > 0 && opStack.Peek() != '(') //only four things go on the opstack
                        {
                            switch (opStack.Pop())
                            {
                                case '*':
                                    outputs.Push(Star.Of(outputs.Pop()));
                                    break;
                                case '+':
                                    right = outputs.Pop();
                                    left = outputs.Pop();
                                    outputs.Push(Concat.Of(left, right));
                                    break;
                                case '|':
                                    right = outputs.Pop();
                                    left = outputs.Pop();
                                    outputs.Push(Union.Of(left, right));
                                    break;
                            }
                        }
                        //not done yet
                        UnityEngine.Debug.Assert(opStack.Peek() == '(');
                        opStack.Pop();
                        break;
                }
            }
            while(opStack.Count > 0)
            {
                switch(opStack.Pop())
                {
                    case '*':
                        outputs.Push(Star.Of(outputs.Pop()));
                        break;
                    case '+':
                        right = outputs.Pop();
                        left = outputs.Pop();
                        outputs.Push(Concat.Of(left, right));
                        break;
                    case '|':
                        right = outputs.Pop();
                        left = outputs.Pop();
                        outputs.Push(Union.Of(left, right));
                        break;
                }
            }
            UnityEngine.Debug.Assert(outputs.Count == 1);
            return outputs.Peek();
        }
        */
    }

    public class Union: OpTree
    {
        public HashSet<OpTree> children = new HashSet<OpTree>();

        protected Union(params OpTree[] children)
        {
            foreach (OpTree child in children)
                if(child != null)
                    AddChild(child);
        }

        public static OpTree Of(OpTree[] children)
        {
            return Of(null, children);
        }

        public static OpTree Of(OpTree child, params OpTree[] children)
        { 
            if (child == null)
                return Of(children[0], children.Where((v, k) => k != 0).ToArray());
            if (children.Length == 0)
                return child;
            child = child.Clone();
            var childOpts = child.GetAllOptions();
            var childrenOpts = children.Select(c => c.GetAllOptions()).ToArray();
            if (childOpts.All(o => childrenOpts.Any(c => c.Contains(o))))
                return Of(null, children);
            if (child is Union)
            {
                Union union = (Union) child;
                foreach(OpTree c in children)
                {
                    union.AddChild(c.Clone());
                }
                return union;
            }
            Union u = new Union(child);
            if (child == null)
                u.children.Clear();
            foreach (OpTree tree in children)
                if(tree != null)
                    u.AddChild(tree.Clone());
            if (u.children.Count == 1)
                return u.children.First().Clone();
            //Remove any now-redundant elements
            for(int i = 0; i < u.children.Count;)
            {
                var curr = u.children.ToArray()[i];
                var others = u.children.Where((v, k) => k != i).ToList();
                if (curr.GetAllOptions().All(o => others.Any(c => c.GetAllOptions().Contains(o)))){
                    u.children.Remove(curr);
                    continue;
                }
                i++;
            }
            if (u.children.Count == 1)
                return u.children.First().Clone();
            return u;
        }

        public bool AddChild(OpTree child)
        {
            if (child is Union)
            {
                bool flag = false;
                foreach (OpTree grandchild in child.GetChildren())
                    flag |= AddChild(grandchild);
                return flag;
            }
            if(children.Any(x => x.IsMatch(child)))
                return false;
            if(child is Concat)
            {
                //Distribute a Concat(Union...) into a Union(Concat...)
                //Represents a Union of probably Concats. Each is the same length as child's children
                List<List<OpTree>> descendants = new List<List<OpTree>>();
                foreach (OpTree opTree in child.GetChildren())
                {
                    if (opTree is Union)
                    {
                        Union u = (Union)opTree;
                        var copies = Enumerable.Repeat(descendants, u.children.Count).ToArray();
                        var treeChildren = u.children.ToArray();
                        for (int i = 0; i < u.children.Count; i++)
                        {
                            for (int j = 0; j < descendants.Count; j++)
                            {
                                copies[i][j].Add(treeChildren[i]);
                            }
                        }
                        descendants.Clear();
                        foreach (var copy in copies)
                            descendants.AddRange(copy);
                    }
                    else
                    {
                        for (int i = 0; i < descendants.Count; i++)
                        {
                            descendants[i].Add(opTree.GetChildren()[i]);
                        }
                    }
                }
                //List<OpTree> unionChildren = new List<OpTree>(descendants.Count);
                for (int i = 0;i < descendants.Count; i++)
                {
                    OpTree result = Concat.Of(descendants[i].ToArray());
                    if(result is Concat)
                        children.Add(result);
                    else
                        AddChild(result);
                }
            }
            if(children.Any(x => x.IsMatch(child)))
                return false;
            children.RemoveWhere(x => child.IsMatch(x));
            children.Add(child);
            MarkChanged();
            return true;
        }

        public override OpTree Clone()
        {
            return new Union(children.ToArray());
        }

        public override List<string> GetAllOptions()
        {
            return children.SelectMany(child => child.GetAllOptions()).ToList();
        }

        public override List<OpTree> GetChildren()
        {
            return children.ToList();
        }

        public override bool IsMatch(OpTree tree)
        {
            return children.Any(child => child.IsMatch(tree));
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is Union;
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return children.Select(child => child.ToFormalRegex()).Aggregate(new FormalRegex(), (x,y) => x.Union(y));
        }
        */
        public override string ToFormalRegexString()
        {
            return children.Select(x => x.ToFormalRegexString()).Aggregate((x,y) => $"{x}|{y}");
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tabCount; i++)
                sb.Append("  ");
            sb.AppendLine("union");
            foreach(OpTree child in children)
                sb.Append(child.ToString(tabCount+1));
            return sb.ToString();
        }

        public override string ToSystemRegex()
        {
            if (children.Count == 0)
                return "";
            if(children.Any(child => child is Epsilon))
            {
                HashSet<OpTree> copy = new HashSet<OpTree>(children);
                copy.RemoveWhere(child => child is Epsilon);
                return copy.Select(child => child.ToSystemRegex()).Aggregate((x, y) => $"{x}|{y}") + "?";
            }
            else
            {
                return children.Select(child => child.ToSystemRegex()).Aggregate((x,y) => $"{x}|{y}");
            }
        }
    }

    public class Concat : OpTree
    {
        public List<OpTree> children;

        protected Concat(params OpTree[] children)
        {
            this.children = children.ToList();
        }

        public static OpTree Of(params OpTree[] children)
        {
            children = children.Where(child => child != null).ToArray();
            if (children.Length > 0 && children.All(child => child is Epsilon))
                return Epsilon.INSTANCE;
            //Epsilon doesn't matter
            children = children.Where(child => !(child is Epsilon)).ToArray();
            if (children.Length == 0)
                return null;
            if(children.Length == 1)
                return children[0];
            List<OpTree> concatChildren = new List<OpTree>();
            foreach(OpTree child in children)
            {
                if(child is Concat)
                    concatChildren.AddRange(child.GetChildren());
                else
                    concatChildren.Add(child);
            }
            return new Concat(concatChildren.ToArray());
        }

        public override OpTree Clone()
        {
            return new Concat(children.ToArray());
        }

        public override List<string> GetAllOptions()
        {
            List<string> options = new List<string>();
            //we need to consider unions and such with their multiple possibilities; we take the cartesian product of them, essentially
            foreach (OpTree child in children)
            {
                List<string> childOpts = child.GetAllOptions();
                if (options.Count == 0)
                    options.AddRange(childOpts);
                else
                {
                    List<List<string>> newOptionSets = Enumerable.Repeat(options, childOpts.Count).ToList();
                    for(int i = 0; i < newOptionSets.Count; i++)
                    {
                        for(int j = 0; j < options.Count; j++)
                        {
                            newOptionSets[i][j] += childOpts[i];
                        }
                    }
                    options.Clear(); //TODO test this
                    options.AddRange(newOptionSets.SelectMany(x => x));
                }
            }
            return options;
        }

        public override List<OpTree> GetChildren()
        {
            return children;
        }

        public override bool IsMatch(OpTree tree)
        {
            if(!(tree is Concat)) return false;
            List<OpTree> otherChildren = tree.GetChildren();
            if(otherChildren.Count != children.Count) return false;
            for (int i = 0; i < children.Count; i++)
            {
                if(!otherChildren[i].IsMatch(this))
                    return false;
            }
            return true;
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is Concat;
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return children.Select(child => child.ToFormalRegex()).Aggregate((x, y) => x.Concat(y));
        }*/

        public override string ToFormalRegexString()
        {
            return children.Select(x => x is Union ? $"({x.ToFormalRegexString()})" : x.ToFormalRegexString()).Aggregate((x, y) => $"{x}{y}");
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tabCount; i++)
                sb.Append("  ");
            sb.AppendLine("concat");
            foreach(OpTree child in children)
                sb.Append(child.ToString(tabCount+1));
            return sb.ToString();
        }

        public override string ToSystemRegex()
        {
            return children.Select(child => child is Union ? "("+child.ToSystemRegex()+")" : child.ToSystemRegex()).Aggregate((x, y) => x+y);
        }
    }

    public class Star: OpTree
    {
        public OpTree child;

        private Star(OpTree child)
        {
            this.child = child;
        }

        public static OpTree Of(OpTree tree)
        {
            if(tree == null) return null;
            if(tree is Union)
            {
                Union union = (Union)tree;
                union.children = new HashSet<OpTree>(union.children.Select(child => child is Star ? child.GetChildren()[0] : child));
                union.children.RemoveWhere(child => child is Epsilon); //epsilon no longer matters inside star
                if (union.children.Count == 1)
                    return Of(union.children.First());
            }
            if (tree is Star || tree is Epsilon) 
                return tree;
            return new Star(tree);
        }

        public override OpTree Clone()
        {
            return new Star(child.Clone());
        }

        /// <summary>
        /// Not meant to be called on Kleene star, due to having infinite options.
        /// </summary>
        /// <returns>The first 20 options, which should be enough</returns>
        public override List<string> GetAllOptions()
        {
            List<string> strings = new List<string>
            {
                ""
            };
            foreach(string add in child.GetAllOptions())
            {
                for (int i = 0; i < 20; i++)
                {
                    strings.Add(strings.Last() + child);
                }
            }
            return strings;
        }

        public override List<OpTree> GetChildren()
        {
            return new List<OpTree>{child};
        }

        public override bool IsMatch(OpTree tree)
        {
            //S* is a subset of R* if and only if S is in R*
            if (tree is Epsilon)
                return true;
            if (child is Union && child.GetChildren().Any(child => child is A) && child.GetChildren().Any(child => child is B))
                return true; //(a|b)* matches all
            if (tree is Star) //If R* matches S, then R* matches S*
                return IsMatch(tree.GetChildren()[0]);
            if (tree is Concat)
            {
                var treeChildren = tree.GetChildren();
                var grandchildren = child.GetChildren();
                if(grandchildren.Count == 0)
                {
                    return treeChildren.All(c => child.IsMatch(c));
                }
                if (treeChildren.Count % grandchildren.Count != 0)
                    return false;
                for(int i = 0; i < treeChildren.Count; i++) {
                    if (!grandchildren[i % grandchildren.Count].IsMatch(treeChildren[i]))
                        return false;
                }
                return true;
            }
            //a or b would only match if the child of the star does
            return child.IsMatch(tree);
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is Star;
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return child.ToFormalRegex().Star();
        }*/

        public override string ToFormalRegexString()
        {
            if(child is Union || child is Concat)
                return $"({child.ToFormalRegexString()})*";
            return child.ToFormalRegexString() + "*";
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < tabCount; i++)
            {
                sb.Append("  ");
            }
            sb.AppendLine("star");
            sb.Append(child.ToString(tabCount+1));
            return sb.ToString();
        }

        public override string ToSystemRegex()
        {
            string str = child.ToSystemRegex();
            if(child is Concat || child is Union)
                str = "(" + str + ")";
            return str + "*";
        }
    }

    public class A: OpTree
    {
        private A() { }
        public static A INSTANCE = new A();

        public override OpTree Clone()
        {
            return INSTANCE;
        }

        public override List<OpTree> GetChildren()
        {
            return new List<OpTree>();
        }

        public override bool IsMatch(OpTree tree)
        {
            return tree is A;
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is A;
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return new FormalRegex("a");
        }*/

        public override string ToSystemRegex()
        {
            return "a";
        }

        public override List<string> GetAllOptions()
        {
            return new List<string>() { "a" };
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < tabCount; i++)
                sb.Append("  ");
            sb.AppendLine("a");
            return sb.ToString();
        }

        public override string ToFormalRegexString()
        {
            return "a";
        }
    }

    public class B: OpTree
    {
        private B() { }
        public static B INSTANCE = new B();

        public override OpTree Clone()
        {
            return INSTANCE;
        }

        public override List<OpTree> GetChildren()
        {
            return new List<OpTree>();
        }

        public override bool IsMatch(OpTree tree)
        {
            return tree is B;
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is B;
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return new FormalRegex("b");
        }*/

        public override string ToSystemRegex()
        {
            return "b";
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tabCount; i++)
                sb.Append("  ");
            sb.AppendLine("b");
            return sb.ToString();
        }

        public override string ToFormalRegexString()
        {
            return "b";
        }

        public override List<string> GetAllOptions()
        {
            return new List<string>() { "b" };
        }
    }

    public class Epsilon: OpTree
    {
        private Epsilon() { }
        public static Epsilon INSTANCE = new Epsilon();

        public override List<OpTree> GetChildren()
        {
            return new List<OpTree>();
        }

        public override string ToSystemRegex()
        {
            return "";
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return new FormalRegex("ε");
        }*/

        public override OpTree Clone()
        {
            return INSTANCE;
        }

        public override bool IsMatch(OpTree tree)
        {
            return tree is Epsilon;
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is Epsilon;
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tabCount; i++)
                sb.Append("  ");
            sb.AppendLine("ε");
            return sb.ToString();
        }

        public override string ToFormalRegexString()
        {
            return "ε";
        }

        public override List<string> GetAllOptions()
        {
            return new List<string>() {""};
        }
    }

    public class EmptyTree : OpTree
    {
        private EmptyTree() { }
        public static EmptyTree INSTANCE = new EmptyTree();

        public override OpTree Clone()
        {
            return this;
        }

        public override List<OpTree> GetChildren()
        {
            return new List<OpTree>();
        }

        public override bool IsMatch(OpTree tree)
        {
            return IsSameType(tree);
        }

        public override bool IsSameType(OpTree tree)
        {
            return tree is EmptyTree;
        }
        /*
        public override FormalRegex ToFormalRegex()
        {
            return new FormalRegex();
        }*/

        public override string ToSystemRegex()
        {
            return null;
        }

        public override string ToString(int tabCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tabCount; i++)
                sb.Append("  ");
            sb.AppendLine("EMPTY");
            return sb.ToString();
        }

        public override string ToFormalRegexString()
        {
            return "EMPTY";
        }

        public override List<string> GetAllOptions()
        {
            return new List<string>();
        }
    }
}