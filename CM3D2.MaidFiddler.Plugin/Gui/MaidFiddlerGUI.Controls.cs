﻿using System;
using System.Linq;
using System.Windows.Forms;
using CM3D2.MaidFiddler.Hook;
using CM3D2.MaidFiddler.Plugin.Utils;
using CBox = System.Windows.Forms.ComboBox;

namespace CM3D2.MaidFiddler.Plugin.Gui
{
    public partial class MaidFiddlerGUI
    {
        private const int PARAMS_COLUMN_VALUE = 1;
        private const int PARAMS_COLUMN_LOCK = 2;
        private const int TABLE_COLUMN_HAS = 0;
        private const int TABLE_COLUMN_LEVEL = 2;
        private const int TABLE_COLUMN_TOTAL_XP = 3;
        private const int SKILL_COLUMN_PLAY_COUNT = 4;
        private bool clearingTables;
        private bool hasPressedEnter;

        public bool ControlsEnabled
        {
            set
            {
                SetAllControlsEnabled(tabControl1, value);
                maidToolStripMenuItem.Enabled = value;
                tabControl1.Enabled = true;
            }
        }

        private void ClearAllFields()
        {
            Debugger.Assert(() => { ClearAllFields(tabControl1); }, "Failed to clear all GUI fields");
        }

        private void ClearAllFields(Control c)
        {
            if (c == tabPage_player)
            {
                foreach (
                DataGridViewCell cell in
                dataGridView_game_params.Rows.Cast<DataGridViewRow>()
                                        .SelectMany(row => row.Cells.Cast<DataGridViewCell>())
                                        .Where(cell => cell.Value is bool))
                {
                    cell.Value = false;
                }
                return;
            }
            foreach (Control control in c.Controls)
            {
                ClearAllFields(control);
            }
            TextBox box = c as TextBox;
            if (box != null)
                box.Clear();
            else
            {
                CheckBox checkBox = c as CheckBox;
                if (checkBox != null)
                    checkBox.Checked = false;
                else
                {
                    CBox comboBox = c as CBox;
                    if (comboBox != null)
                        comboBox.SelectedIndex = -1;
                    else
                    {
                        DataGridView view = c as DataGridView;
                        if (view == null)
                            return;
                        clearingTables = true;
                        foreach (DataGridViewCell cell in
                        view.Rows.Cast<DataGridViewRow>().SelectMany(row => row.Cells.Cast<DataGridViewCell>()))
                        {
                            if (cell.Value is bool)
                                cell.Value = false;
                            else if (cell.Value is int)
                                cell.Value = 0;
                            else if (cell.Value is long)
                                cell.Value = 0L;
                            else if (cell.Value is uint)
                                cell.Value = (uint) 0;
                        }
                        clearingTables = false;
                    }
                }
            }
        }

        private void InitControl(Control control)
        {
            if (control is TextBox)
            {
                control.LostFocus += OnControlLostFocus;
                control.KeyPress += OnControlKeyPress;
            }
            else
            {
                CBox box = control as CBox;
                if (box != null)
                {
                    CBox b = box;
                    b.SelectedIndexChanged += OnSelectedIndexChanged;
                }
                else
                {
                    CheckBox checkBox = control as CheckBox;
                    if (checkBox == null)
                        return;
                    CheckBox cb = checkBox;
                    cb.CheckStateChanged += OnCheckStateChanged;
                }
            }
        }

        private void InitField(Label label, Control control, MaidChangeType type)
        {
            if (label != null)
                Translation.AddTranslatableControl(label);
            if (control is CheckBox)
                Translation.AddTranslatableControl(control);
            uiControls.Add(control, type);

            InitControl(control);
        }

        private void InitField(Label label, Control control, PlayerChangeType type)
        {
            if (label != null)
                Translation.AddTranslatableControl(label);
            if (control is CheckBox)
                Translation.AddTranslatableControl(control);
            uiControlsPlayer.Add(control, type);

            InitControl(control);
        }

        private void OnCheckStateChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox) sender;
            Debugger.WriteLine($"Checked status change! New value: {cb.Checked}");
            UpdateGameValue(cb, cb.Checked);
        }

        private void OnControlKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r')
                return;
            hasPressedEnter = true;
            tableLayoutPanel1.Focus();
            Control c = (Control) sender;
            UpdateGameValue(c, c.Text);
            e.Handled = true;
        }

        private void OnControlLostFocus(object sender, EventArgs e)
        {
            Control c = (Control) sender;
            if (!hasPressedEnter)
                UpdateGameValue(c, c.Text);
            hasPressedEnter = false;
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            CBox b = (CBox) sender;
            if (b.SelectedIndex >= 0)
                UpdateGameValue(b, b.SelectedIndex);
        }

        private void SetAllControlsEnabled(Control c, bool enabled)
        {
            if (c == tabPage_player)
                return;
            foreach (Control control in c.Controls)
            {
                SetAllControlsEnabled(control, enabled);
            }
            if (c is TabPage)
                c.Enabled = true;
            else
                c.Enabled = enabled;
        }

        private void UpdateGameValue(Control c, object value)
        {
            if (uiControls.ContainsKey(c))
            {
                MaidInfo maid = SelectedMaid;
                if (maid == null)
                    return;
                Debugger.Assert(
                () =>
                {
                    MaidChangeType type = uiControls[c];
                    if (type == MaidChangeType.YotogiClassType)
                        value = EnumHelper.EnabledYotogiClasses[(int) value];
                    Debugger.WriteLine(
                    LogLevel.Info,
                    $"Attempting to update value {type} to {value}. Allowed: {!valueUpdate[type]}.");
                    if (!valueUpdate[type])
                        maid.SetValue(type, value);
                    valueUpdate[type] = false;
                },
                $"Failed to set maid value for {maid.Maid.Param.status.first_name} {maid.Maid.Param.status.last_name}");
            }
            else if (uiControlsPlayer.ContainsKey(c))
            {
                if (Player == null)
                    return;
                Debugger.Assert(
                () =>
                {
                    PlayerChangeType type = uiControlsPlayer[c];
                    Debugger.WriteLine(
                    LogLevel.Info,
                    $"Attempting to update player value {type} to {value}. Allowed: {!valueUpdatePlayer[type]}.");
                    if (!valueUpdatePlayer[type])
                        Player.SetValue(type, value);
                    valueUpdatePlayer[type] = false;
                },
                "Failed to set player value");
            }
        }
    }
}