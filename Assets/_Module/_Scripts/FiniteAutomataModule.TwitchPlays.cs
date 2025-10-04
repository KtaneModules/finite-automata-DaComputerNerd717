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
            if (state < 0)
            {
                yield return "sendtochaterror State must be positive!";
                yield break;
            }
            yield return null;
            var toggle = ToggleGoal(state-1); //-1 because state 1 is row 0
            while (toggle.MoveNext())
            {
                yield return toggle.Current;
            }
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
            if (state < 0)
            {
                yield return "sendtochaterror State must be positive!";
                yield break;
            }
            yield return null;
            var toggle = ToggleStart(state-1); //-1 because state 1 is row 0
            while (toggle.MoveNext())
            {
                yield return toggle.Current;
            }
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
            if (state < 0)
            {
                yield return "sendtochaterror Invalid page! Page must be positive!";
                yield break;
            }
            else
            {
                yield return null;
                var setPage = SetPage(state);
                while(setPage.MoveNext())
                {
                    yield return setPage.Current;
                }
                yield break;
            }
        }
        Match transitionMatch = transitionRegex.Match(command);
        if (transitionMatch.Success)
        {
            int destination;
            if (!int.TryParse(transitionMatch.Groups[1].Value, out state))
            {
                yield return "sendtochaterror Start must be a number!";
                yield break;
            }
            if (state < 0)
            {
                yield return "sendtochaterror Start must be positive!";
                yield break;
            }
            if (!int.TryParse(transitionMatch.Groups[3].Value, out destination))
            {
                yield return "sendtochaterror Destination must be a number!";
                yield break;
            }
            if (destination < 0)
            {
                yield return "sendtochaterror Destination must be positive!";
                yield break;
            }
            if(destination >= tableRows.Count)
            {
                var scroll = ScrollToRow(destination);
                while (scroll.MoveNext())
                {
                    yield return scroll.Current;
                }
            }
            yield return null;
            var setEdge = SetTransition(state-1, transitionMatch.Groups[2].Value.EqualsIgnoreCase("a"), destination);
            while (setEdge.MoveNext())
            {
                yield return setEdge.Current;
            }
            yield break;
        }
        Match submitMatch = submitRegex.Match(command);
        if (submitMatch.Success)
        {
            yield return null;
            var setPage = SetPage(REGEX_PAGES);
            while (setPage.MoveNext()) {
                yield return setPage.Current; 
            }
            yield return _submitButton;
            yield return null;
            yield return _submitButton;
            yield break;
        }
        //if transition didn't match, that probably should mean that the command specified an invalid regex
        if (command.StartsWith("transition"))
        {
            yield return "sendtochaterror Could not determine transition data. Transition regex must be A or B";
            yield break;
        }
        yield return "sendtochaterror Invalid command!";
    }

    private IEnumerator TwitchHandleForcedSolve() {
        //Go to the table page
        page = REGEX_PAGES;
        RedrawPage();
        _module.HandlePass();
        yield return null;
    }
#pragma warning restore IDE0051
// * Declare any TP helper methods here.

    private IEnumerator SetPage(int page)
    {
        while (this.page != page)
        {
            yield return _rightButton;
            yield return null;
            yield return _rightButton; //release button
            yield return null;
        }
        yield break;
    }

    /// <summary>
    /// Guarantees the requested row will be on screen. Follow with a check to currentRowDisplayed == row to identify where the requested row is
    /// </summary>
    /// <param name="row">The requested row, 0-indexed</param>
    /// <returns></returns>
    private IEnumerator ScrollToRow(int row)
    {
        int currentRow = currentRowDisplayed;
        if(currentRow == row && row > 0) //row in top of display, we want to put the row in the bottom if possible
        {
            yield return _up_zone;
            yield return null;
            yield return _up_zone;
            yield return null;
        }
        else if(currentRow + 1 < row) //requested row is below the display
        {
            while(currentRow++ + 1 < row)
            {
                yield return _down_zone;
                yield return null;
                yield return _down_zone;
                yield return null;
            }
        }
        else if (currentRow > row) //requested row is above the display
        {
            while(currentRow-- > row)
            {
                //Log($"Going up from row {currentRow}");
                yield return _up_zone;
                yield return null;
                yield return _up_zone;
                yield return null;
            }
            if (row > 0) //put the row in the bottom if possible
            {
                //Log("Going up one more time");
                yield return _up_zone;
                yield return null;
                yield return _up_zone;
                yield return null;
            }
        }
    }

    private IEnumerator ToggleStartHelper(bool top)
    {
        int row = top ? currentRowDisplayed : currentRowDisplayed + 1;
        int currentFlag = tableRows[row][0];
        while (tableRows[row][0] != (currentFlag ^ startFlag))
        {
            yield return top ? _label1_zone : _label2_zone;
            yield return null;
            yield return top ? _label1_zone : _label2_zone; //release button
            yield return null;
        }
    }

    private IEnumerator ToggleGoalHelper(bool top)
    {
        int row = top ? currentRowDisplayed : currentRowDisplayed + 1;
        int currentFlag = tableRows[row][0];
        while (tableRows[row][0] != (currentFlag ^ goalFlag))
        {
            yield return top ? _label1_zone : _label2_zone;
            yield return null;
            yield return top ? _label1_zone : _label2_zone; //release button
            yield return null;
        }
    }

    private IEnumerator SetTransitionHelper(bool top, bool a, int value) {
        int row = top ? currentRowDisplayed : currentRowDisplayed + 1;
        int rowDex = a ? 2 : 3;
        while (tableRows[row][rowDex] != value)
        {
            yield return a ? (top ? _a1_zone : _a2_zone) : (top ? _b1_zone : _b2_zone);
            yield return null;
            yield return a ? (top ? _a1_zone : _a2_zone) : (top ? _b1_zone : _b2_zone);
            yield return null;
        }
    }

    private IEnumerator ToggleStart(int row)
    {
        var set_page = SetPage(REGEX_PAGES);
        while (set_page.MoveNext())
        {
            yield return set_page.Current;
        }
        var scroll = ScrollToRow(row);
        while (scroll.MoveNext())
        {
            yield return scroll.Current;
        }
        var toggle = ToggleStartHelper(row == 0);
        while (toggle.MoveNext())
        {
            yield return toggle.Current;
        }
    }

    private IEnumerator ToggleGoal(int row)
    {
        var set_page = SetPage(REGEX_PAGES);
        while (set_page.MoveNext())
        {
            yield return set_page.Current;
        }
        var scroll = ScrollToRow(row);
        while (scroll.MoveNext())
        {
            yield return scroll.Current;
        }
        var toggle = ToggleGoalHelper(row == 0);
        while (toggle.MoveNext())
        {
            yield return toggle.Current;
        }
    }

    private IEnumerator SetTransition(int from, bool settingA, int to)
    {
        var set_page = SetPage(REGEX_PAGES);
        while (set_page.MoveNext())
        {
            yield return set_page.Current;
        }
        var scroll = ScrollToRow(from);
        while (scroll.MoveNext())
        {
            yield return scroll.Current;
        }
        var set = SetTransitionHelper(from == 0, settingA, to);
        while (set.MoveNext())
        {
            yield return set.Current;
        }
    }
}