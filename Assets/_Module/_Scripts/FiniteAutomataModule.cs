using DCN.FiniteAutomata;
using KModkit; // You must import this namespace to use KMBombInfoExtensions, among other things. See KModKit Docs below.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
// ! Remember to remove things that you do not use, including using directives and empty methods.

// * Template Wiki: https://github.com/TheKuroEver/KTaNE-Module-Template/wiki
// * KModKit Documentation: https://github.com/Qkrisi/ktanemodkit/wiki
// ! Remember that the class and file names have to match.
[RequireComponent(typeof(KMBombModule), typeof(KMSelectable))]
public partial class FiniteAutomataModule : MonoBehaviour
{
    private KMBombInfo _bombInfo; // for accessing edgework, and certain events like OnBombExploded.
    // private KMAudio _audio; // for interacting with the game's audio system.
    private KMBombModule _module;
    private static int s_moduleCount;
    private int _moduleId;
    private KMSelectable _selectable;

    private const int MAX_REGEX_LENGTH = 30, MIN_REGEX_LENGTH = 10, REGEX_PAGES = 5;

    OpTree[] regexes = new OpTree[REGEX_PAGES];
    TreeNFA correctDFA = null;
    int page = 0;
    /// <summary>
    /// Rows contain 4 elements: Flags, origin, A destination, B destination. Flags are defined as (2 if goal) + (1 if start). Destination=0 is blank.
    /// </summary>
    List<int[]> tableRows = new List<int[]>();
    const int goalFlag = 2, startFlag = 1;
    int currentRowDisplayed = 0;

    [SerializeField]
    private KMSelectable _leftButton;
    [SerializeField]
    private KMSelectable _rightButton;
    [SerializeField]
    private KMSelectable _submitButton;

    [SerializeField]
    private Text _text;
    [SerializeField]
    private KMSelectable _a1_zone;
    [SerializeField]
    private KMSelectable _a2_zone;
    [SerializeField]
    private KMSelectable _b1_zone;
    [SerializeField]
    private KMSelectable _b2_zone;
    [SerializeField]
    private KMSelectable _label1_zone;
    [SerializeField]
    private KMSelectable _label2_zone;
    [SerializeField]
    private KMSelectable _up_zone;
    [SerializeField]
    private KMSelectable _down_zone;

    [SerializeField]
    private KMSelectable _moduleSelect;

    private KMSelectable[] allChildren;
    private KMSelectable[] regexChildren;

    const int ROWS_DISPLAYED = 2;

    //public static uint iterationCount = 0;

#pragma warning disable IDE0051

    // * Called before anything else.
    private void Awake() {
        //*
        //Debug.Log("Awake called");
        _moduleId = s_moduleCount++;

        _module = GetComponent<KMBombModule>();
        _bombInfo = GetComponent<KMBombInfo>(); // (*)
        // _audio = GetComponent<KMAudio>();

        _leftButton.OnInteract += TurnPageLeft;
        _rightButton.OnInteract += TurnPageRight;
        _submitButton.OnInteract += Submit;
        //_screen.OnInteract += OnScreenTouch;
        _a1_zone.OnInteract += OnA1Touch;
        //_a1_zone.enabled = false;
        _a2_zone.OnInteract += OnA2Touch;
        //_a2_zone.enabled = false;
        _b1_zone.OnInteract += OnB1Touch;
        //_b1_zone.enabled = false;
        _b2_zone.OnInteract += OnB2Touch;
        //_b2_zone.enabled = false;
        _label1_zone.OnInteract += OnLabel1Touch;
        //_label1_zone.enabled = false;
        _label2_zone.OnInteract += OnLabel2Touch;
        //_label2_zone.enabled = false;
        _up_zone.OnInteract += OnUpTouch;
        //_up_zone.enabled = false;
        _down_zone.OnInteract += OnDownTouch;
        //_down_zone.enabled = false;

        _module.OnActivate += Activate;

        allChildren = (KMSelectable[])_moduleSelect.Children.Clone();
        regexChildren = new KMSelectable[] { allChildren[0], allChildren[1], allChildren[2] };
        // _bombInfo.OnBombExploded += OnBombExploded; // (**). Requires (*)
        // _bombInfo.OnBombSolved += OnBombSolved; // (***). Requires (*)

        // * Declare other references here if needed.
        //*/
    }

    // * Called after Awake has been called on all components in the scene, but before anything else.
    // ! Things like querying edgework need to be done after Awake is called, eg. subscribing to OnInteract events.
    private void Start() {
        //Debug.Log("Start called");
        //*
        tableRows.Add(new int[] { 0, 1, 0, 0 });
        tableRows.Add(new int[] { 0, 2, 0, 0 });
        for (int i = 0; i < 5; i++)
        {
            //if (iterationCount++ > 100000)
            //{
            //    UnityEngine.Debug.LogError("Infinite Loop");
            //    return;
            //}
            OpTree tree = GenerateOpTree();
            //Debug.Log($"Tree {i} generated\n"+tree.ToFormalRegexString());
            Debug.Assert(tree != null);
            //tree.Flatten();
            Debug.Assert(tree != null);

            regexes[i] = tree;
            Debug.Assert(regexes[i] != null);
        }
        RedrawPage();
        /*
        _text.text =  "┌──────┰─┬─┐\n"+
                      "│origin┃a│b│\n"+
                      "┝━━━━━━╋━┿━┥\n"+
                      "│   1  ┃2│1│\n"+
                      "├──────╂─┼─┤\n"+
                      "│   2  ┃1│2│\n"+
                      "└──────┸─┴─┘";
        //*/
        
        //*
        IEnumerable<int> digits = _bombInfo.GetSerialNumberNumbers();
        //IEnumerable<int> digits = new int[] { 1, 3, 0, 7};
        if ((digits.Count() & 1) == 1) { //odd number of digits, solution based on middle digit
            int dex = digits.ElementAt((digits.Count() - 1) / 2);
            bool complement = dex > 4;
            dex %= 5;
            //Mark this or the complement as the correct one, set the answer FA to this one's
            correctDFA = new TreeNFA(regexes[dex]);
            //Debug.Log("TG:");
            //Debug.Log(correctDFA.ToString());
            correctDFA = correctDFA.ToDFA();
            //Debug.Log(correctDFA.ToString());
            if (!correctDFA.isValid) {
                Debug.LogError("DFA invalid (single case)");
                Debug.LogError(correctDFA.ToString());
                return;
            }
            if (complement)
            {
                //Debug.Log("Complementing");
                correctDFA = correctDFA.DoComplement();
            }
            Debug.Log("Final correct DFA: " + correctDFA.ToString());
        }
        else{ //even number of digits, solution based on first and last digits
            int firstDex = digits.First();
            int secondDex = digits.Last();
            bool doUnion = firstDex <= secondDex; //if nondecreasing, use union, else intersection
            bool complementFirst = firstDex > 4;
            firstDex %= 5;
            bool complementSecond = secondDex > 4;
            secondDex %= 5;
            //Compute the operation and set the answer as the result's FA
            TreeNFA dfa1 = new TreeNFA(regexes[firstDex]);
            //Debug.Log("TG 1");
            //Debug.Log(dfa1.ToString());
            dfa1 = dfa1.ToDFA();
            if (!dfa1.isValid) {
                Debug.LogError("DFA1 invalid");
                Debug.LogError(dfa1.ToString());
                return;
            }
            //Debug.Log("DFA 1");
            //Debug.Log(dfa1.ToString());
            TreeNFA dfa2 = new TreeNFA(regexes[secondDex]);
            //Debug.Log("TF 2");
            //Debug.Log(dfa2.ToString());
            dfa2 = dfa2.ToDFA();
            if (!dfa2.isValid)
            {
                Debug.LogError("DFA2 invalid");
                Debug.LogError(dfa2.ToString());
                return;
            }
            //Debug.Log("DFA 2");
            //Debug.Log(dfa2.ToString());
            if (complementFirst)
                dfa1 = dfa1.DoComplement();
            if (complementSecond)
                dfa2 = dfa2.DoComplement();
            //Debug.Log("Before operation");
            //Debug.Log(dfa1.ToString());
            //Debug.Log(dfa2.ToString());
            if (doUnion)
            {
                //Debug.Log("Union:");
                correctDFA = dfa1.DoUnion(dfa2);
            }
            else
            {
                //Debug.Log("Intersection:");
                correctDFA = dfa1.DoIntersection(dfa2);
            }
            Log("Final correct DFA: " + correctDFA.ToString());
        }
        //*/
    }

    // * Called once the lights turn on.
    private void Activate() { }

    // * Update is called every frame. I don't typically use Update in the main script.
    // ! Do not perform resource-intensive tasks here as they will be called every frame and can slow the game down.
    private void Update() { }

    // * Called when the module is removed from the game world.
    // * Examples of when this happens include when the bomb explodes, or if the player quits to the office.
    private void OnDestroy() { }
#pragma warning restore IDE0051

    // private void OnBombExploded() { } // Requires (*) and (**)
    // private void OnBombSolved() { } // Requires (*) and (***)

    public void Log(string message) => Debug.Log($"[{_module.ModuleDisplayName} #{_moduleId}] {message}");

    public void Strike(string message) {
        Log($"✕ {message}");
        _module.HandleStrike();
        // * Add code that should execute on every strike (eg. a strike animation) here.
    }

    public void Solve() {
        Log("◯ Module solved!");
        _module.HandlePass();
        // * Add code that should execute on solve (eg. a solve animation) here.
    }

    //TODO make the result more fair
    private OpTree GenerateOpTree()
    {
        return GenerateOpTree(GetLength(ProbFunction));
    }

    private OpTree GenerateOpTree(int length)
    {
        OpTree tree;
        if(length < 2)
        {
            int symbolDex = UnityEngine.Random.Range(0, 3);
            switch(symbolDex)
            {
                case 0:
                    tree = A.INSTANCE;
                    break;
                case 1:
                    tree = B.INSTANCE;
                    break;
                case 2:
                    tree = Epsilon.INSTANCE;
                    break;
                default: //shouldn't happen
                    tree = EmptyTree.INSTANCE;
                    break;
            }
            if (tree == null)
                Debug.LogError("how");
            //Debug.Log("Generated tree:\n"+tree.ToString());
            return tree;
        }

       
        int childLen;
        OpTree child1, child2;
        do
        {
            int opDex = UnityEngine.Random.Range(0, 3);
            switch (opDex)
            {
                case 0:
                    //childLen = UnityEngine.Random.Range(1, length);
                    OpTree child = GenerateOpTree(length - 1);
                    tree = Star.Of(child);
                    //Debug.Log("Generated tree:\n" + tree.ToString());
                    break;
                case 1:
                    childLen = UnityEngine.Random.Range(1, length);
                    child1 = GenerateOpTree(childLen);
                    child2 = GenerateOpTree(length - childLen);
                    tree = Union.Of(child1, child2);
                    break;
                case 2:
                    childLen = UnityEngine.Random.Range(1, length);
                    child1 = GenerateOpTree(childLen);
                    child2 = GenerateOpTree(length - childLen);
                    tree = Concat.Of(child1, child2);
                    break;
                default: //shouldn't happen
                    tree = EmptyTree.INSTANCE;
                    break;
            }
        } while (tree.GetOpCount() < length);
        //Debug.Log("Generated tree:\n" + tree.ToString());
        return tree;
    }

    delegate double ProbFunc(double len);

    private int GetLength(ProbFunc probFunc)
    {
        int len = 0;
        for (; UnityEngine.Random.Range(0f, 1f) <= probFunc(len); len++)
        {
            /*
            if (iterationCount++ > 100000)
            {
                UnityEngine.Debug.LogError("Infinite Loop");
                return -1;
            }
            */
        }
        return len;
    }

    private double ProbFunction(double x){
        //max(0, (1/max(1, x-9)) - 1/21)^0.3
        return Math.Pow(Math.Max(0, (1 / Math.Max(1, x - MIN_REGEX_LENGTH + 1)) - 1 / (MAX_REGEX_LENGTH - MIN_REGEX_LENGTH + 1)), 0.3);
    }

    private bool TurnPageLeft()
    {
        //Log("Left button pressed");
        page--;
        if (page < 0) page += (REGEX_PAGES + 1);
        //Log("Going to page " + page);
        RedrawPage();
        return false;
    }

    private bool TurnPageRight()
    {
        //Log("Right button pressed");
        page++;
        if(page > REGEX_PAGES) page -= (REGEX_PAGES + 1);
        //Log("Going to page " + page);
        RedrawPage();
        return false;
    }

    private bool isRowValid(int[] row)
    {
        return row.Length == 4 && row[1] > 0 && row[2] > 0 && row[3] > 0;
    }
    
    private bool Submit()
    {
        //Log("Submit button pressed");
        //interpret the entries and check
        TreeNFA submittedTree = new TreeNFA();
        //List<int[]> validRows = tableRows.Where(row => row.Length == 4 && row[1]>0 && row[2]>0 && row[3]>0).OrderBy(row => row[1]).ToList();
        //Log("Valid rows determined");
        for(int i = 0; i < tableRows.Count; i++)
        {
            //if (iterationCount++ > 10000)
            //{
            //    //Debug.LogError("Infinite loop");
            //    return;
            //}
            int[] row = tableRows[i];
            if (isRowValid(row))
                submittedTree.AddNode((row[0] & startFlag) != 0, (row[0] & goalFlag) != 0);
            else
                submittedTree.AddNode(); //spacer so we don't have to adjust references
            //Log($"Added node: {submittedTree.GetNode(i).start} {submittedTree.GetNode(i).goal} {row[1]} {row[2]} {row[3]}");
        }
        for(int i = 0; i < tableRows.Count; i++)
        {
            //if (iterationCount++ > 10000)
            //{
            //    //Debug.LogError("Infinite loop");
            //    return;
            //}
            //UnityEngine.Debug.Assert(iterationCount++ < 100000);
            int[] row = tableRows[i];
            if (!isRowValid(row))
                continue;
            submittedTree.AddEdge(row[1]-1, row[2]-1, A.INSTANCE); //1-indexed to 0-indexed
            //Log($"Connected node {row[1]} to node {row[2]}");
            submittedTree.AddEdge(row[1]-1, row[3]-1, B.INSTANCE);
            //Log($"Connected node {row[1]} to node {row[3]}");
        }
        //Debug.Assert(submittedTree.GetNode(0).start);
        //Log("Connections done");
        //Log("Trimming spacer nodes");)
        submittedTree.TrimDisconnected();
        //Log("Testing equality");
        string reason;
        if (submittedTree.IsEqual(correctDFA, out reason))
        {
            //Log("Correct answer submitted: " + reason);
            Solve();
        }
        else
        {
            //Log("Incorrect answer submitted: " + reason);
            Strike($"Incorrect answer: {reason}");
        }
        return false;
    }

    private bool OnA1Touch()
    {
        //Log("A1 Touched");
        if (page == REGEX_PAGES)
        {
            int temp = tableRows[currentRowDisplayed][2];
            temp++;
            temp %= tableRows.Count + 1;
            tableRows[currentRowDisplayed][2] = temp;
            RedrawPage();
        }
        return false;
    }

    private bool OnA2Touch()
    {
        //Log("A2 Touched");
        if (page == REGEX_PAGES)
        {
            int temp = tableRows[currentRowDisplayed+1][2];
            temp++;
            temp %= tableRows.Count + 1;
            tableRows[currentRowDisplayed + 1][2] = temp;
            RedrawPage();
        }
        return false;
    }

    private bool OnB1Touch()
    {
        //Log("B1 Touched");
        if (page == REGEX_PAGES)
        {
            int temp = tableRows[currentRowDisplayed][3];
            temp++;
            temp %= tableRows.Count+1;
            tableRows[currentRowDisplayed][3] = temp;
            RedrawPage();
        }
        return false;
    }

    private bool OnB2Touch()
    {
        //Log("B2 Touched");
        if (page == REGEX_PAGES)
        {
            int temp = tableRows[currentRowDisplayed + 1][3];
            temp++;
            temp %= tableRows.Count+1;
            tableRows[currentRowDisplayed + 1][3] = temp;
            RedrawPage();
        }
        return false;
    }

    private bool OnLabel1Touch()
    {
        //Log("Label 1 Touched");
        if (page == REGEX_PAGES)
        {
            int temp = tableRows[currentRowDisplayed][0];
            temp++;
            temp %= 4;
            tableRows[currentRowDisplayed][0] = temp;
            RedrawPage();
        }
        return false;
    }

    private bool OnLabel2Touch()
    {
        //Log("Label 2 Touched");
        if (page == REGEX_PAGES)
        {
            int temp = tableRows[currentRowDisplayed+1][0];
            temp++;
            temp %= 4;
            tableRows[currentRowDisplayed+1][0] = temp;
            RedrawPage();
        }
        return false;
    }
    
    private bool OnUpTouch()
    {
        //Log("Up Touched");
        if (page == REGEX_PAGES)
        {
            if(currentRowDisplayed > 0)
            {
                currentRowDisplayed--;
                RedrawPage();
            }
        }
        return false;
    }

    private bool OnDownTouch()
    {
        //Log("Down Touched");
        if (page == REGEX_PAGES)
        {
            
            if(currentRowDisplayed >= tableRows.Count - ROWS_DISPLAYED)
            {
                tableRows.Add(new int[]{ 0, tableRows.Count+1, 0, 0});
            }
            currentRowDisplayed++;
            RedrawPage();
        }
        return false;
    }

    private void RedrawPage()
    {
        if (page == REGEX_PAGES)
        {
            _moduleSelect.Children = (KMSelectable[])allChildren.Clone();
            for (int i = 3; i < allChildren.Length; i++)
            {
                //if (iterationCount++ > 10000)
                //{
                //    UnityEngine.Debug.LogError("Infinite loop");
                //    return;
                //}
                //allChildren[i].Parent = _moduleSelect;
                //allChildren[i].enabled = true;
                allChildren[i].gameObject.SetActive(true);
            }
            _moduleSelect.UpdateChildrenProperly();
            string table = BuildTableRows(currentRowDisplayed, 2);
            //Log(table);
            _text.text = table;
        }
        else
        {
            _moduleSelect.Children = (KMSelectable[])regexChildren.Clone();
            for (int i = 3; i < allChildren.Length; i++)
            {
                //if (iterationCount++ > 10000)
                //{
                //    UnityEngine.Debug.LogError("Infinite loop");
                //    return;
                //}
                //allChildren[i].Parent = null;
                allChildren[i].gameObject.SetActive(false);
            }
            _moduleSelect.UpdateChildrenProperly();
            string text = $"{page}\n{regexes[page].ToFormalRegexString()}";
            //Debug.Log(text);
            _text.text = text;
            
        }
    }

    const string blCorner = "└",      brCorner = "┘",        trCorner = "┐",         tlCorner = "┌",
                 vert = "│",          vertHeavy = "┃",       horiz = "─",            horizHeavy = "━",
                 tBottomHeavy = "┸",  tTopHeavy = "┰",       tLeftHeavy = "┝",       tRightHeavy = "┥",
                 tBottom = "┴",       tTop = "┬",            tRight = "┤",           tLeft = "├",
                 cross = "┼",         crossHeavyVert = "╂",  crossHeavyHoriz = "┿",  crossHeavy = "╋";

    private string BuildTableRows(int rowStart, int rowCount)
    {
        //iterationCount = 0;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(PadToLength("↑", 17));
        sb.AppendLine("┌───────┰───┬───┐");
        sb.AppendLine("│ Origin┃ a │ b │");
        bool first = true;
        for (int i = rowStart; i < rowStart + rowCount; i++)
        {
            //if (iterationCount++ > 10000)
            //{
            //    UnityEngine.Debug.LogError("Infinite loop");
            //    return null;
            //}
            //UnityEngine.Debug.Assert(iterationCount++ < 100000);
            if (i >= tableRows.Count)
                break;
            int[] row = tableRows[i];
            if (first)
            {
                first = false;
                sb.AppendLine("┝━━━━━━━╋━━━┿━━━┥");
            }
            else {
                sb.AppendLine("├───────╂───┼───┤");
            }
            StringBuilder eleBuilder = new StringBuilder();
            if ((row[0] & startFlag) != 0)
                eleBuilder.Append(">");
            if ((row[0] & goalFlag) != 0) 
                eleBuilder.Append("+");
            eleBuilder.Append(tableRows[i][1]);
            sb.Append("│");
            sb.Append(PadToLength(eleBuilder.ToString(), 7));
            sb.Append("┃");
            sb.Append(PadToLength(row[2] <= 0 ? "" : row[2].ToString(), 3));
            sb.Append("│");
            sb.Append(PadToLength(row[3] <= 0 ? "" : row[3].ToString(), 3));
            sb.AppendLine("│");
        }
        sb.AppendLine("└───────┸───┴───┘");
        sb.Append(PadToLength("↓", 17));
        return sb.ToString();
    }

    private string PadToLength(string str, int length)
    {
        int spacesToAdd = length - str.Length;
        if (spacesToAdd == 0)
            return str;
        int rightSpaces = spacesToAdd / 2;
        int leftSpaces = spacesToAdd - rightSpaces;
        string result = "";
        for (int i = 0; i < leftSpaces; i++)
        {
            //if (iterationCount++ > 10000)
            //{
            //    UnityEngine.Debug.LogError("Infinite loop");
            //    return null;
            //}
            result += " ";
        }
            
        result += str;
        for(int i = 0;i < rightSpaces; i++)
        {
            //if (iterationCount++ > 10000)
            //{
            //    UnityEngine.Debug.LogError("Infinite loop");
            //    return null;
            //}
            result += " ";
        }
        return result;
    }
}
