﻿/*
 * Copyright © 2016-2019 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 *
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExtendedControls
{
    // Represents a check list, with optional group options, and standard options.  Tags/Text are separated.

    public class CheckedIconListBoxFilterForm
    {
        public Action<string,Object> Closing;                       // Action on close, string is the settings.
        public bool AllOrNoneBack { get; set; } = true;            // use to control if ALL or None is reported, else its all entries or empty list
        public Action<CheckedIconListBoxFilterForm, ItemCheckEventArgs> CheckedChanged;       // when any tick has changed
        public bool CloseOnDeactivate { get; set; } = true;         // close when deactivated - this would be normal behaviour

        private ExtendedControls.CheckedIconListBoxForm cc;
        private Object tagback;

        private class Options
        {
            public string Tag;
            public string Text;
            public Image Image;
        }

        private List<Options> groupoptions = new List<Options>();
        private List<Options> standardoptions = new List<Options>();

        public void AddGroupOption(string tags, string text, Image img = null)      // group option
        {
            groupoptions.Add(new Options() { Tag = tags, Text = text, Image = img });
        }

        public void AddStandardOption(string tag, string text, Image img = null)                // standard option
        {
            standardoptions.Add(new Options() { Tag = tag, Text = text, Image = img });
        }

        public void AddStandardOption(List<Tuple<string,string,Image>> list)                // standard option
        {
            foreach (var x in list)
                AddStandardOption(x.Item1, x.Item2, x.Item3);
        }

        public void SortStandardOptions()
        {
            standardoptions.Sort(delegate (Options left, Options right)     // in order, oldest first
            {
                return left.Text.CompareTo(right.Text);
            });
        }

        // present below control
        public void Filter(string settings, Control ctr, Form parent,  Object tag = null, bool applytheme = true, int width = -1, bool allnone = true)
        {
            Filter(settings, ctr.PointToScreen(new Point(0, ctr.Size.Height)), new Size(width < 1 ? (ctr.Width * 3) : width, 600), parent, tag, applytheme, allnone);
        }

        public void Filter(string settings, Point p, Size s, Form parent, Object tag = null, bool applytheme = true, bool allnone = true)
        {
            if (cc == null)
            {
                cc = new ExtendedControls.CheckedIconListBoxForm();

                if (allnone)
                {
                    AddGroupOption("All", "All".Tx(), Properties.Resources.All);       // displayed, translate
                    AddGroupOption("None", "None".Tx(), Properties.Resources.None);
                }
                    
                foreach (var x in groupoptions)
                    cc.AddItem(x.Tag, x.Text, x.Image);

                foreach (var x in standardoptions)
                    cc.AddItem(x.Tag, x.Text, x.Image);

                string[] slist = settings.SplitNoEmptyStartFinish(';');
                if (slist.Length == 1 && slist[0].Equals("All"))
                    cc.SetCheckedFromToEnd(groupoptions.Count());
                else
                    cc.SetChecked(settings);

                SetFilterSet();

                cc.FormClosed += FilterClosed;
                cc.CheckedChanged += checkboxchanged;
                cc.PositionSize(p, s);
                cc.LargeChange = cc.ItemCount * Properties.Resources.All.Height / 40;   // 40 ish scroll movements
                cc.CloseOnDeactivate = CloseOnDeactivate;
                if (applytheme)
                    ThemeableFormsInstance.Instance?.ApplyToControls(cc, applytothis: true);

                tagback = tag;

                cc.Show(parent);
            }
            else
                cc.Close();
        }

        private void SetFilterSet()
        {
            string list = cc.GetChecked(groupoptions.Count());       // using All or None.. on items beyond reserved entries
                                                                //            System.Diagnostics.Debug.WriteLine("Checked" + list);
            int p = 2;
            foreach (var eo in groupoptions)
            {
                if ( eo.Tag == "All")
                {
                    cc.SetChecked(p, list.Equals("All"));
                }
                else if ( eo.Tag == "None")
                {
                    cc.SetChecked(p, list.Equals("None"));
                }
                else if (list.MatchesAllItemsInList(eo.Tag,';'))        // exactly, tick
                {
                    //System.Diagnostics.Debug.WriteLine("Checking T " + eo.Tag + " vs " + list);
                    cc.SetChecked(p);
                }
                else if (list.ContainsAllItemsInList(eo.Tag, ';')) // contains, intermediate
                {
                    //System.Diagnostics.Debug.WriteLine("Checking I " + eo.Tag + " vs " + list + list.Equals(eo.Tag));
                    cc.SetChecked(p, CheckState.Indeterminate);
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Checking F " + eo.Tag + " vs " + list);
                    cc.SetChecked(p, false);
                }

                p++;
            }
        }

        private void checkboxchanged(CheckedIconListBoxForm sender, ItemCheckEventArgs e)          // called after check changed for the new system
        {
            if (e.NewValue == CheckState.Checked)       // all or none set all of them
            {
                if (e.Index < groupoptions.Count())
                {
                    bool shift = Control.ModifierKeys.HasFlag(Keys.Control);

                    if ( !shift )
                        cc.SetCheckedFromToEnd(groupoptions.Count(), false);   // if not shift, we clear all, and apply this tag

                    string tag = groupoptions[e.Index].Tag;
                    if ( tag == "All")
                        cc.SetCheckedFromToEnd(groupoptions.Count(), true);
                    else if ( tag == "None ")
                        cc.SetCheckedFromToEnd(groupoptions.Count(), false);
                    else
                        cc.SetChecked(tag);
                }
            }
            else
            {
                if ( e.Index < groupoptions.Count())        // off on this clears the entries of it only
                {
                    string tag = groupoptions[e.Index].Tag;
                    if ( tag != "All" && tag != "None ")
                        cc.SetChecked(tag, false);
                }
            }

            SetFilterSet();
            CheckedChanged?.Invoke(this, e);
        }

        private void FilterClosed(Object sender, FormClosedEventArgs e)
        {
            Closing?.Invoke(cc.GetChecked(groupoptions.Count,AllOrNoneBack),tagback);
            cc = null;
        }
    }
}
