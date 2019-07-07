﻿using System;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight.Command;
using OfficeRibbonXEditor.Controls.Forms;
using OfficeRibbonXEditor.Interfaces;
using OfficeRibbonXEditor.Models;
using ScintillaNET;

namespace OfficeRibbonXEditor.ViewModels
{
    public class FindReplaceDialogViewModel : DialogBase, IContentDialog<ValueTuple<Scintilla, FindReplaceAction>>
    {
        private CharacterRange searchRange = new CharacterRange();

        public FindReplaceDialogViewModel()
        {
            this.FindNextCommand = new RelayCommand(() => this.FindWrapper(false));
            this.FindPreviousCommand = new RelayCommand(() => this.FindWrapper(true));
            this.FindAllCommand = new RelayCommand(this.ExecuteFindAllCommand);
            this.ReplaceNextCommand = new RelayCommand(() => this.ReplaceWrapper(false));
            this.ReplacePreviousCommand = new RelayCommand(() => this.ReplaceWrapper(true));
            this.ClearCommand = new RelayCommand(this.ExecuteClearCommand);
        }

        public RecentListViewModel<string> RecentFinds { get; } = new RecentListViewModel<string>();

        public RecentListViewModel<string> RecentReplaces { get; } = new RecentListViewModel<string>();

        public FindReplace FindReplace { get; private set; }

        public IncrementalSearcher IncrementalSearcher { get; private set; }

        private Scintilla scintilla;

        public Scintilla Scintilla
        {
            get => this.scintilla;
            set
            {
                var previous = this.scintilla;
                if (!this.Set(ref this.scintilla, value))
                {
                    return;
                }

                if (previous != null && this.IncrementalSearcher != null)
                {
                    previous.Controls.Remove(this.IncrementalSearcher);
                }

                this.FindReplace = new FindReplace(this.scintilla);

                this.IncrementalSearcher = new IncrementalSearcher
                {
                    Scintilla = this.Scintilla,
                    FindReplace = this,
                    Visible = false
                };
                this.Scintilla.Controls.Add(this.IncrementalSearcher);
            }
        }

        public RelayCommand FindNextCommand { get; }

        public RelayCommand FindPreviousCommand { get; }

        public RelayCommand FindAllCommand { get; }

        public RelayCommand ReplaceNextCommand { get; }

        public RelayCommand ReplacePreviousCommand { get; }

        public RelayCommand ClearCommand { get; }

        private bool isFindTabSelected = true;

        public bool IsFindTabSelected
        {
            get => this.isFindTabSelected;
            set => this.Set(ref this.isFindTabSelected, value);
        }

        private bool isStandardSearch = true;

        public bool IsStandardSearch
        {
            get => this.isStandardSearch;
            set => this.Set(ref this.isStandardSearch, value);
        }

        private bool isExtendedSearch;

        public bool IsExtendedSearch
        {
            get => this.isExtendedSearch;
            set => this.Set(ref this.isExtendedSearch, value);
        }

        private bool isRegExSearch;

        public bool IsRegExSearch
        {
            get => this.isRegExSearch;
            set => this.Set(ref this.isRegExSearch, value);
        }

        private bool ignoreCase;

        public bool IgnoreCase
        {
            get => this.ignoreCase;
            set => this.Set(ref this.ignoreCase, value);
        }

        private bool wholeWord;

        public bool WholeWord
        {
            get => this.wholeWord;
            set => this.Set(ref this.wholeWord, value);
        }

        private bool wordStart;

        public bool WordStart
        {
            get => this.wordStart;
            set => this.Set(ref this.wordStart, value);
        }

        private bool isCompiled;

        public bool IsCompiled
        {
            get => this.isCompiled;
            set => this.Set(ref this.isCompiled, value);
        }

        private bool isCultureInvariant;

        public bool IsCultureInvariant
        {
            get => this.isCultureInvariant;
            set => this.Set(ref this.isCultureInvariant, value);
        }

        private bool isEcmaScript;

        public bool IsEcmaScript
        {
            get => this.isEcmaScript;
            set => this.Set(ref this.isEcmaScript, value);
        }

        private bool isExplicitCapture;

        public bool IsExplicitCapture
        {
            get => this.isExplicitCapture;
            set => this.Set(ref this.isExplicitCapture, value);
        }

        private bool ignorePatternWhitespace;

        public bool IgnorePatternWhitespace
        {
            get => this.ignorePatternWhitespace;
            set => this.Set(ref this.ignorePatternWhitespace, value);
        }

        private bool isMultiline;

        public bool IsMultiline
        {
            get => this.isMultiline;
            set => this.Set(ref this.isMultiline, value);
        }

        private bool isRightToLeft;

        public bool IsRightToLeft
        {
            get => this.isRightToLeft;
            set => this.Set(ref this.isRightToLeft, value);
        }

        private bool isSingleLine;

        public bool IsSingleLine
        {
            get => this.isSingleLine;
            set => this.Set(ref this.isSingleLine, value);
        }

        private bool wrap;

        public bool Wrap
        {
            get => this.wrap;
            set => this.Set(ref this.wrap, value);
        }

        private bool searchSelection;

        public bool SearchSelection
        {
            get => this.searchSelection;
            set => this.Set(ref this.searchSelection, value);
        }

        private bool markLine;

        public bool MarkLine
        {
            get => this.markLine;
            set => this.Set(ref this.markLine, value);
        }

        private bool highlightMatches;

        public bool HighlightMatches
        {
            get => this.highlightMatches;
            set => this.Set(ref this.highlightMatches, value);
        }

        private string findText;

        public string FindText
        {
            get => this.findText;
            set => this.Set(ref this.findText, value);
        }

        private string replaceText;

        public string ReplaceText
        {
            get => this.replaceText;
            set => this.Set(ref this.replaceText, value);
        }

        private string statusText;

        public string StatusText
        {
            get => this.statusText;
            set => this.Set(ref this.statusText, value);
        }

        private void ExecuteFindAllCommand()
        {
            if (string.IsNullOrEmpty(this.FindText))
            {
                return;
            }

            this.StatusText = string.Empty;

            this.ExecuteClearCommand();
            var foundCount = 0;

            if (this.IsRegExSearch)
            {
                Regex rr = null;
                try
                {
                    rr = new Regex(this.FindText, this.GetRegexOptions());
                }
                catch (ArgumentException ex)
                {
                    this.StatusText = $"Error in Regular Expression: {ex.Message}";
                    return;
                }

                if (this.SearchSelection)
                {
                    if (this.searchRange.cpMin == this.searchRange.cpMax)
                    {
                        this.searchRange = new CharacterRange(this.scintilla.Selections[0].Start, this.scintilla.Selections[0].End);
                    }

                    foundCount = this.FindReplace.FindAll(this.searchRange, rr, this.MarkLine, this.HighlightMatches).Count;
                }
                else
                {
                    this.searchRange = new CharacterRange();
                    foundCount = this.FindReplace.FindAll(rr, this.MarkLine, this.HighlightMatches).Count;
                }
            }
            else
            {
                if (this.SearchSelection)
                {
                    if (this.searchRange.cpMin == this.searchRange.cpMax) this.searchRange = new CharacterRange(this.scintilla.Selections[0].Start, this.scintilla.Selections[0].End);

                    var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                    foundCount = this.FindReplace.FindAll(this.searchRange, textToFind, this.GetSearchFlags(), this.MarkLine, this.HighlightMatches).Count;
                }
                else
                {
                    this.searchRange = new CharacterRange();
                    var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                    foundCount = this.FindReplace.FindAll(textToFind, this.GetSearchFlags(), this.MarkLine, this.HighlightMatches).Count;
                }
            }

            this.StatusText = $"Total found: {foundCount}";
            this.RecentFinds.Add(this.FindText);
        }

        private void ExecuteClearCommand()
        {
            this.Scintilla.MarkerDeleteAll(this.FindReplace.Marker.Index);
            this.FindReplace.ClearAllHighlights();
        }

        private void FindWrapper(bool searchUp)
        {
            if (string.IsNullOrEmpty(this.FindText))
            {
                return;
            }

            this.StatusText = string.Empty;

            CharacterRange foundRange;

            try
            {
                foundRange = this.FindNext(searchUp);
            }
            catch (ArgumentException ex)
            {
                this.StatusText = $"Error in Regular Expression: {ex.Message}";
                return;
            }

            if (foundRange.cpMin == foundRange.cpMax)
            {
                this.StatusText = "Match could not be found";
            }
            else
            {
                if ((searchUp && foundRange.cpMin < this.Scintilla.AnchorPosition) || (!searchUp && foundRange.cpMin > this.Scintilla.CurrentPosition))
                {
                    this.StatusText = $"Search match wrapped to the beginning of the {(this.SearchSelection ? "selection" : "document")}";
                }

                this.Scintilla.SetSel(foundRange.cpMin, foundRange.cpMax);
                this.MoveDialogAwayFromSelection();
            }

            this.RecentFinds.Add(this.FindText);
        }

        private CharacterRange FindNext(bool searchUp)
        {
            Regex rr = null;
            return this.FindNext(searchUp, ref rr);
        }

        private CharacterRange FindNext(bool searchUp, ref Regex rr)
        {
            CharacterRange foundRange;

            if (this.IsRegExSearch)
            {
                rr = new Regex(this.FindText, this.GetRegexOptions());

                if (this.SearchSelection)
                {
                    if (this.searchRange.cpMin == this.searchRange.cpMax) this.searchRange = new CharacterRange(this.scintilla.Selections[0].Start, this.scintilla.Selections[0].End);

                    if (searchUp)
                        foundRange = this.FindReplace.FindPrevious(rr, this.Wrap, this.searchRange);
                    else
                        foundRange = this.FindReplace.FindNext(rr, this.Wrap, this.searchRange);
                }
                else
                {
                    this.searchRange = new CharacterRange();
                    if (searchUp)
                        foundRange = this.FindReplace.FindPrevious(rr, this.Wrap);
                    else
                        foundRange = this.FindReplace.FindNext(rr, this.Wrap);
                }
            }
            else
            {
                if (this.SearchSelection)
                {
                    if (this.searchRange.cpMin == this.searchRange.cpMax) this.searchRange = new CharacterRange(this.scintilla.Selections[0].Start, this.scintilla.Selections[0].End);

                    if (searchUp)
                    {
                        var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                        foundRange = this.FindReplace.FindPrevious(textToFind, this.Wrap, this.GetSearchFlags(), this.searchRange);
                    }
                    else
                    {
                        var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                        foundRange = this.FindReplace.FindNext(textToFind, this.Wrap, this.GetSearchFlags(), this.searchRange);
                    }
                }
                else
                {
                    this.searchRange = new CharacterRange();
                    if (searchUp)
                    {
                        var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                        foundRange = this.FindReplace.FindPrevious(textToFind, this.Wrap, this.GetSearchFlags());
                    }
                    else
                    {
                        var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                        foundRange = this.FindReplace.FindNext(textToFind, this.Wrap, this.GetSearchFlags());
                    }
                }
            }

            return foundRange;
        }

        private void ReplaceWrapper(bool searchUp)
        {
            if (string.IsNullOrEmpty(this.FindText))
            {
                return;
            }

            this.StatusText = string.Empty;

            CharacterRange nextRange;
            try
            {
                nextRange = this.ReplaceNext(searchUp);
            }
            catch (ArgumentException ex)
            {
                this.StatusText = $"Error in Regular Expression: {ex.Message}";
                return;
            }

            if (nextRange.cpMin == nextRange.cpMax)
            {
                this.StatusText = "Match could not be found";
            }
            else
            {
                if (nextRange.cpMin < this.Scintilla.AnchorPosition)
                {
                    this.StatusText = $"Search match wrapped to the beginning of the {(this.SearchSelection ? "selection" : "document")}";
                }

                this.Scintilla.SetSel(nextRange.cpMin, nextRange.cpMax);
                this.MoveDialogAwayFromSelection();
            }

            this.RecentFinds.Add(this.FindText);
            this.RecentReplaces.Add(this.ReplaceText);
        }

        private CharacterRange ReplaceNext(bool searchUp)
        {
            Regex rr = null;
            var selRange = new CharacterRange(this.scintilla.Selections[0].Start, this.scintilla.Selections[0].End);

            //	We only do the actual replacement if the current selection exactly
            //	matches the find.
            if (selRange.cpMax - selRange.cpMin > 0)
            {
                if (this.IsRegExSearch)
                {
                    rr = new Regex(this.FindText, this.GetRegexOptions());
                    var selRangeText = this.Scintilla.GetTextRange(selRange.cpMin, selRange.cpMax - selRange.cpMin + 1);

                    if (selRange.Equals(this.FindReplace.Find(selRange, rr, false)))
                    {
                        //	If searching up we do the replacement using the range object.
                        //	Otherwise we use the selection object. The reason being if
                        //	we use the range the caret is positioned before the replaced
                        //	text. Conversely if we use the selection object the caret will
                        //	be positioned after the replaced text. This is very important
                        //	because we don't want the new text to be potentially matched
                        //	in the next search.
                        if (searchUp)
                        {
                            this.scintilla.SelectionStart = selRange.cpMin;
                            this.scintilla.SelectionEnd = selRange.cpMax;
                            this.scintilla.ReplaceSelection(rr.Replace(selRangeText, this.ReplaceText));
                            this.scintilla.GotoPosition(selRange.cpMin);
                        }
                        else
                            this.Scintilla.ReplaceSelection(rr.Replace(selRangeText, this.ReplaceText));
                    }
                }
                else
                {
                    var textToFind = this.IsExtendedSearch ? this.FindReplace.Transform(this.FindText) : this.FindText;
                    if (selRange.Equals(this.FindReplace.Find(selRange, textToFind, false)))
                    {
                        //	If searching up we do the replacement using the range object.
                        //	Otherwise we use the selection object. The reason being if
                        //	we use the range the caret is positioned before the replaced
                        //	text. Conversely if we use the selection object the caret will
                        //	be positioned after the replaced text. This is very important
                        //	becuase we don't want the new text to be potentially matched
                        //	in the next search.
                        if (searchUp)
                        {
                            var textToReplace = this.IsExtendedSearch ? this.FindReplace.Transform(this.ReplaceText) : this.ReplaceText;
                            this.scintilla.SelectionStart = selRange.cpMin;
                            this.scintilla.SelectionEnd = selRange.cpMax;
                            this.scintilla.ReplaceSelection(textToReplace);

                            this.scintilla.GotoPosition(selRange.cpMin);
                        }
                        else
                        {
                            var textToReplace = this.IsExtendedSearch ? this.FindReplace.Transform(this.ReplaceText) : this.ReplaceText;
                            this.Scintilla.ReplaceSelection(textToReplace);
                        }
                    }
                }
            }

            this.RecentFinds.Add(this.FindText);
            this.RecentReplaces.Add(this.ReplaceText);

            return this.FindNext(searchUp, ref rr);
        }

        public RegexOptions GetRegexOptions()
        {
            var ro = RegexOptions.None;

            if (this.IsCompiled)
            {
                ro |= RegexOptions.Compiled;
            }

            if (this.IsCultureInvariant)
            {
                ro |= RegexOptions.Compiled;
            }

            if (this.IsEcmaScript)
            {
                ro |= RegexOptions.ECMAScript;
            }

            if (this.IsExplicitCapture)
            {
                ro |= RegexOptions.ExplicitCapture;
            }

            if (this.IgnoreCase)
            {
                ro |= RegexOptions.IgnoreCase;
            }

            if (this.IgnorePatternWhitespace)
            {
                ro |= RegexOptions.IgnorePatternWhitespace;
            }

            if (this.IsMultiline)
            {
                ro |= RegexOptions.Multiline;
            }

            if (this.IsRightToLeft)
            {
                ro |= RegexOptions.RightToLeft;
            }

            if (this.IsSingleLine)
            {
                ro |= RegexOptions.Singleline;
            }

            return ro;
        }

        public SearchFlags GetSearchFlags()
        {
            var sf = SearchFlags.None;

            if (!this.IgnoreCase)
            {
                sf |= SearchFlags.MatchCase;
            }

            if (this.WholeWord)
            {
                sf |= SearchFlags.WholeWord;
            }

            if (this.WordStart)
            {
                sf |= SearchFlags.WordStart;
            }

            return sf;
        }

        public bool OnLoaded((Scintilla, FindReplaceAction) payload)
        {
            var (editor, action) = payload;
            this.Scintilla = editor;

            switch (action)
            {
                case FindReplaceAction.Find:
                    this.IsFindTabSelected = true;
                    break;
                case FindReplaceAction.Replace:
                    this.IsFindTabSelected = false;
                    break;
                case FindReplaceAction.FindNext:
                    this.FindWrapper(false);
                    return false;
                case FindReplaceAction.FindPrevious:
                    this.FindWrapper(true);
                    return false;
                case FindReplaceAction.IncrementalSearch:
                    this.IncrementalSearcher?.Show();
                    return false;
                default:
                    throw new NotImplementedException($"Unhandled loading action: {action}");
            }

            return true;
        }

        public virtual void MoveDialogAwayFromSelection()
        {
            // TODO: This might be tricky to implement in MVVM
            // Most sensible solution would be moving the code to the view, and invoke it via an event
        }
    }
}
