using System.Collections;
using System.Text.RegularExpressions;

#pragma warning disable IDE1006
// ! This name must match the name in the main class file.
partial class FiniteAutomataModule
{
    // * TP Documentation: https://github.com/samfundev/KtaneTwitchPlays/wiki/External-Mod-Module-Support
    // ! Remove if not used. For more niche things like TwitchManualCode and ZenModeActive, look at tp docs ^^
    //private bool TwitchPlaysActive;
    private bool TwitchShouldCancelCommand;
#pragma warning disable 414, IDE0051
    private readonly string TwitchHelpMessage = @"Use '!{0} goal (state)' to toggle if the state is a goal. | '!{0} start (state)' to toggle if the state is a start | '!{0} transition (state) (a or b) (destination)' to set the transition for a or b to go to the specified destination | '!{0} page [0-6]' to go to that page | '!{0} submit' to submit";
#pragma warning restore 414, IDE1006
    Regex goalRegex = new Regex(@"goal (\d+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    Regex startRegex = new Regex(@"start (\d+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    Regex pageRegex = new Regex(@"page (\d+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    Regex transitionRegex = new Regex(@"transition (\d+) ([ab]) (\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    Regex submitRegex = new Regex("submit", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private IEnumerator ProcessTwitchCommand(string command) {
        command = command.Trim().ToUpperInvariant();

        Match goalMatch = goalRegex.Match(command);
        int state;
        if (goalMatch.Success) {
            if(!int.TryParse(goalMatch.Groups[1].Value, out state))
            {
                yield return "sendtochaterror State must be a number!";
                yield break;
            }
            yield return null;
            AddNewStatesIfNeeded(state);
            tableRows[state-1][0] ^= goalFlag; //-1 since the tableRows are 0-indexed but states are 1-indexed
            currentRowDisplayed = System.Math.Max(state - 2, 0);
            //if they requested state 3, that's index 2, so we can show indices 1 and 2
            //This way, if they requested the last one, say there's 5 and they requested 5, we see 3 and 4
            //Naturally, if they put something too small, we want to show the first one
            page = REGEX_PAGES;
            RedrawPage();
            yield break;
        }
        Match startMatch = startRegex.Match(command);
        if(startMatch.Success)
        {
            if(!int.TryParse(startMatch.Groups[1].Value, out state))
            {
                yield return "sendtochaterror State must be a number!";
                yield break;
            }
            yield return null;
            AddNewStatesIfNeeded(state);
            tableRows[state-1][0] ^= startFlag;
            currentRowDisplayed = System.Math.Max(state - 2, 0);
            //if they requested state 3, that's index 2, so we can show indices 1 and 2
            //This way, if they requested the last one, say there's 5 and they requested 5, we see 3 and 4
            //Naturally, if they put something too small, we want to show the first one
            page = REGEX_PAGES;
            RedrawPage();
            yield break;
        }
        Match pageMatch = pageRegex.Match(command);
        if(pageMatch.Success)
        {
            if(!int.TryParse(pageMatch.Groups[1].Value, out state))
            {
                yield return "sendtochaterror Page must be a number!";
                yield break;
            }
            else if(state > REGEX_PAGES)
            {
                yield return $"sendtochaterror Invalid page! Page must be at most {REGEX_PAGES}";
                yield break;
            }
            else
            {
                yield return null;
                page = state;
                RedrawPage();
                yield break;
            }
        }
        Match transitionMatch = transitionRegex.Match(command);
        if (transitionMatch.Success)
        {
            int destination;
            if (!int.TryParse(transitionMatch.Groups[1].Value, out state))
            {
                yield return "sendtochaterror State must be a number!";
                yield break;
            }
            if (!int.TryParse(transitionMatch.Groups[3].Value, out destination))
            {
                yield return "sendtochaterror Destination must be a number!";
                yield break;
            }
            AddNewStatesIfNeeded(state);
            AddNewStatesIfNeeded(destination);
            if (transitionMatch.Groups[2].Value.EqualsIgnoreCase("a"))
                tableRows[state - 1][2] = destination - 1;
            else
                tableRows[state - 1][3] = destination - 1;
            page = REGEX_PAGES;
            RedrawPage();
        }
        Match submitMatch = submitRegex.Match(command);
        if (submitMatch.Success)
        {
            yield return null;
            page = REGEX_PAGES;
            Submit();
            yield break;
        }
        yield return "sendtochaterror Invalid command!";
        yield break;
    }

    private IEnumerator TwitchHandleForcedSolve() {
        //Log("TP autosolver has not yet been implemented. Calling KMBombModule.HandlePass.");
        //Go to the table page
        page = REGEX_PAGES;
        RedrawPage();
        _module.HandlePass();
        yield return null;
    }
#pragma warning restore IDE0051
// * Declare any TP helper methods here.

    private void AddNewStatesIfNeeded(int newStateDex)
    {   
        while(newStateDex - 1 >= tableRows.Count) //say they put 5, we need to have 0-4 since tableRows[4] is 5
            tableRows.Add(new int[] { 0, tableRows.Count + 1, 0, 0 });
    }
}