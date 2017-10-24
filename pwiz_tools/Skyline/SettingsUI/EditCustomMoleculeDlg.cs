﻿/*
 * Original author: Max Horowitz-Gelb <maxhg .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pwiz.Common.SystemUtil;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public partial class EditCustomMoleculeDlg : FormEx
    {
        private CustomMolecule _resultCustomMolecule;
        private Adduct _resultAdduct;
        private readonly FormulaBox _formulaBox;
        private readonly Identity _initialId;
        private readonly IEnumerable<Identity> _existingIds;
        private readonly int _minCharge;
        private readonly int _maxCharge;
        private readonly TransitionSettings _transitionSettings;
        private PeptideSettings _peptideSettings;
        private readonly PeptideSettingsUI.LabelTypeComboDriver _driverLabelType;
        private readonly SkylineWindow _parent;

        public enum UsageMode
        {
            moleculeNew,
            moleculeEdit,
            precursor,
            fragment
        }

        /// <summary>
        /// For modifying at the Molecule level
        /// </summary>
        public EditCustomMoleculeDlg(SkylineWindow parent, string title,
            SrmSettings settings, CustomMolecule molecule, ExplicitRetentionTimeInfo explicitRetentionTime) :
            this(parent, UsageMode.moleculeEdit, title, null, null, 0, 0, null, molecule, Adduct.EMPTY, null,
                explicitRetentionTime, null)
        {
        }

        /// <summary>
        /// For creating at the Molecule level (create molecule and first transition group) or modifying at the transition level
        /// Null values imply "don't ask user for this"
        /// </summary>
        public EditCustomMoleculeDlg(SkylineWindow parent, UsageMode usageMode, string title, Identity initialId,
            IEnumerable<Identity> existingIds, int minCharge, int maxCharge,
            SrmSettings settings, CustomMolecule molecule, Adduct defaultCharge,
            ExplicitTransitionGroupValues explicitAttributes,
            ExplicitRetentionTimeInfo explicitRetentionTime,
            IsotopeLabelType defaultIsotopeLabelType)
        {
            Text = title;
            _parent = parent;
            _initialId = initialId;
            _existingIds = existingIds;
            _minCharge = minCharge;
            _maxCharge = maxCharge;
            _transitionSettings = settings != null ? settings.TransitionSettings : null;
            _peptideSettings = settings != null ? settings.PeptideSettings : null;
            _resultAdduct = Adduct.EMPTY;

            var enableFormulaEditing = usageMode == UsageMode.moleculeNew || usageMode == UsageMode.moleculeEdit ||
                                       usageMode == UsageMode.fragment;
            var enableAdductEditing = usageMode == UsageMode.moleculeNew || usageMode == UsageMode.precursor ||
                                      usageMode == UsageMode.fragment;
            var suggestOnlyAdductsWithMass = usageMode != UsageMode.fragment;

            InitializeComponent();

            NameText = molecule == null ? String.Empty : molecule.Name;
            textName.Enabled = usageMode == UsageMode.moleculeNew || usageMode == UsageMode.moleculeEdit ||
                               usageMode == UsageMode.fragment; // Can user edit name?

            var needOptionalValuesBox = explicitRetentionTime != null || explicitAttributes != null;
            var heightDelta = 0;

            // Initialise the ion mobility units dropdown with L10N values
            foreach (MsDataFileImpl.eIonMobilityUnits t in Enum.GetValues(typeof(MsDataFileImpl.eIonMobilityUnits)))
                comboBoxIonMobilityUnits.Items.Add(IonMobilityFilter.IonMobilityUnitsL10NString(t));

            if (explicitAttributes == null)
            {
                ResultExplicitTransitionGroupValues = null;
                labelCollisionEnergy.Visible = false;
                textCollisionEnergy.Visible = false;
                labelSLens.Visible = false;
                textSLens.Visible = false;
                labelCompensationVoltage.Visible = false;
                textCompensationVoltage.Visible = false;
                labelCCS.Visible = false;
                textBoxCCS.Visible = false;
                labelConeVoltage.Visible = false;
                textConeVoltage.Visible = false;
                labelIonMobilityHighEnergyOffset.Visible = false;
                textIonMobilityHighEnergyOffset.Visible = false;
                labelIonMobility.Visible = false;
                textIonMobility.Visible = false;
                labelIonMobilityUnits.Visible = false;
                comboBoxIonMobilityUnits.Visible = false;
                if (needOptionalValuesBox)
                {
                    // We blanked out everything but the retention time
                    var vmargin = labelRetentionTime.Location.Y;
                    var newHeight = textRetentionTime.Location.Y + textRetentionTime.Height + vmargin;
                    heightDelta = groupBoxOptionalValues.Height - newHeight;
                    groupBoxOptionalValues.Height = newHeight;
                }
            }
            else
            {
                ResultExplicitTransitionGroupValues = new ExplicitTransitionGroupValues(explicitAttributes);
            }

            string labelAverage = !defaultCharge.IsEmpty
                ? Resources.EditCustomMoleculeDlg_EditCustomMoleculeDlg_A_verage_m_z_
                : Resources.EditCustomMoleculeDlg_EditCustomMoleculeDlg_A_verage_mass_;
            string labelMono = !defaultCharge.IsEmpty
                ? Resources.EditCustomMoleculeDlg_EditCustomMoleculeDlg__Monoisotopic_m_z_
                : Resources.EditCustomMoleculeDlg_EditCustomMoleculeDlg__Monoisotopic_mass_;
            var defaultFormula = molecule == null ? string.Empty : molecule.Formula;
            var transition = initialId as Transition;

            FormulaBox.EditMode editMode;
            if (enableAdductEditing && !enableFormulaEditing)
                editMode = FormulaBox.EditMode.adduct_only;
            else if (!enableAdductEditing && enableFormulaEditing)
                editMode = FormulaBox.EditMode.formula_only;
            else
                editMode = FormulaBox.EditMode.formula_and_adduct;
            string formulaBoxLabel;
            if (defaultCharge.IsEmpty)
            {
                formulaBoxLabel = Resources.EditCustomMoleculeDlg_EditCustomMoleculeDlg_Chemi_cal_formula_;
            }
            else if (editMode == FormulaBox.EditMode.adduct_only)
            {
                var prompt = defaultFormula;
                if (string.IsNullOrEmpty(defaultFormula) && molecule != null)
                {
                    // Defined by mass only
                    prompt = molecule.ToString();
                }
                formulaBoxLabel = string.Format(Resources.EditCustomMoleculeDlg_EditCustomMoleculeDlg_Addu_ct_for__0__,
                    prompt);
            }
            else
            {
                formulaBoxLabel = Resources.EditMeasuredIonDlg_EditMeasuredIonDlg_Ion__chemical_formula_;
            }

            double? averageMass = null;
            double? monoMass = null;
            if (transition != null && string.IsNullOrEmpty(defaultFormula) && transition.IsCustom())
            {
                averageMass = transition.CustomIon.AverageMass;
                monoMass = transition.CustomIon.MonoisotopicMass;
            }
            else if (molecule != null)
            {
                averageMass = molecule.AverageMass;
                monoMass = molecule.MonoisotopicMass;
            }

            _formulaBox =
                new FormulaBox(false, // Not proteomic, so offer Cl and Br in atoms popup
                    formulaBoxLabel,
                    labelAverage,
                    labelMono,
                    defaultCharge,
                    editMode,
                    suggestOnlyAdductsWithMass)
                {
                    NeutralFormula = defaultFormula,
                    AverageMass = averageMass,
                    MonoMass = monoMass,
                    Location = new Point(textName.Left, textName.Bottom + 12)
                };
            _formulaBox.ChargeChange += (sender, args) =>
            {
                if (!_formulaBox.Adduct.IsEmpty)
                {
                    Adduct = _formulaBox.Adduct;
                    var revisedFormula = _formulaBox.NeutralFormula + Adduct.AdductFormula;
                    if (!Equals(revisedFormula, _formulaBox.Formula))
                    {
                        _formulaBox.Formula = revisedFormula;
                    }
                    if (string.IsNullOrEmpty(_formulaBox.NeutralFormula) && averageMass.HasValue)
                    {
                        _formulaBox.AverageMass = averageMass;
                        _formulaBox.MonoMass = monoMass;
                    }
                }
            };
            Controls.Add(_formulaBox);
            _formulaBox.TabIndex = 2;
            _formulaBox.Enabled = enableFormulaEditing || enableAdductEditing;
            Adduct = defaultCharge;
            var needCharge = !Adduct.IsEmpty;
            textCharge.Visible = labelCharge.Visible = needCharge;
            if (needOptionalValuesBox && !needCharge)
            {
                heightDelta += groupBoxOptionalValues.Location.Y - labelCharge.Location.Y;
                groupBoxOptionalValues.Location = new Point(groupBoxOptionalValues.Location.X, labelCharge.Location.Y);
            }
            if (explicitRetentionTime == null)
            {
                // Don't ask user for retetention times
                RetentionTime = null;
                RetentionTimeWindow = null;
                labelRetentionTime.Visible = false;
                labelRetentionTimeWindow.Visible = false;
                textRetentionTime.Visible = false;
                textRetentionTimeWindow.Visible = false;
                if (needOptionalValuesBox)
                {
                    var rtHeight = labelCollisionEnergy.Location.Y - labelRetentionTimeWindow.Location.Y;
                    groupBoxOptionalValues.Height -= rtHeight;
                    heightDelta += rtHeight;
                }
            }
            else
            {
                RetentionTime = explicitRetentionTime.RetentionTime;
                RetentionTimeWindow = explicitRetentionTime.RetentionTimeWindow;
            }
            if (!needOptionalValuesBox)
            {
                groupBoxOptionalValues.Visible = false;
                heightDelta = groupBoxOptionalValues.Height;
            }
            // Initialize label
            if (settings != null && defaultIsotopeLabelType != null)
            {
                _driverLabelType = new PeptideSettingsUI.LabelTypeComboDriver(comboIsotopeLabelType,
                    settings.PeptideSettings.Modifications, null, null, null, null)
                {
                    SelectedName = defaultIsotopeLabelType.Name
                };
            }
            else
            {
                comboIsotopeLabelType.Visible = false;
                labelIsotopeLabelType.Visible = false;
            }
            Height -= heightDelta;
        }

        public CustomMolecule ResultCustomMolecule
        {
            get { return _resultCustomMolecule; }
        }

        public Adduct ResultAdduct
        {
            get { return _resultAdduct; }
        }

        public void SetResult(CustomMolecule mol, Adduct adduct)
        {
            _resultCustomMolecule = mol;
            _resultAdduct = adduct;
            SetNameAndFormulaBoxText();
        }

        public ExplicitTransitionGroupValues ResultExplicitTransitionGroupValues
        {
            get
            {
                return new ExplicitTransitionGroupValues(CollisionEnergy, IonMobility, IonMobilityHighEnergyOffset,
                    IonMobilityUnits,
                    CollisionalCrossSectionSqA,
                    SLens, ConeVoltage, DeclusteringPotential, CompensationVoltage);
            }
            set
            {
                // Use constructor to handle value == null
                var resultExplicitTransitionGroupValues = new ExplicitTransitionGroupValues(value);
                CollisionEnergy = resultExplicitTransitionGroupValues.CollisionEnergy;
                IonMobility = resultExplicitTransitionGroupValues.IonMobility;
                IonMobilityHighEnergyOffset = resultExplicitTransitionGroupValues.IonMobilityHighEnergyOffset;
                IonMobilityUnits = resultExplicitTransitionGroupValues.IonMobilityUnits;
                CollisionalCrossSectionSqA = resultExplicitTransitionGroupValues.CollisionalCrossSectionSqA;
                SLens = resultExplicitTransitionGroupValues.SLens;
                ConeVoltage = resultExplicitTransitionGroupValues.ConeVoltage;
                DeclusteringPotential = resultExplicitTransitionGroupValues.DeclusteringPotential;
                CompensationVoltage = resultExplicitTransitionGroupValues.CompensationVoltage;
            }
        }

        public ExplicitRetentionTimeInfo ResultRetentionTimeInfo
        {
            get
            {
                return RetentionTime.HasValue
                    ? new ExplicitRetentionTimeInfo(RetentionTime.Value, RetentionTimeWindow)
                    : null;
            }
            set
            {
                if (value != null)
                {
                    RetentionTime = value.RetentionTime;
                    RetentionTimeWindow = value.RetentionTimeWindow;
                }
                else
                {
                    RetentionTime = null;
                    RetentionTimeWindow = null;
                }
            }
        }

        public Adduct Adduct
        {
            get
            {
                if (!_formulaBox.Adduct.IsEmpty)
                    return _formulaBox.Adduct;
                Adduct val;
                if (Adduct.TryParse(textCharge.Text, out val))
                    return val;
                return Adduct.EMPTY;
            }
            set
            {
                _formulaBox.Adduct = value;
                if (value.IsEmpty)
                {
                    textCharge.Text = string.Empty;
                }
                else
                {
                    textCharge.Text =
                        value.AdductCharge.ToString(LocalizationHelper
                            .CurrentCulture); // If adduct is "M+Na", show charge as "1"
                }
            }
        }

        private static double? NullForEmpty(string text)
        {
            double val;
            if (double.TryParse(text, out val))
                return val;
            return null;
        }

        private static string EmptyForNullOrNonPositive(double? value)
        {
            double dval = (value ?? 0);
            return (dval <= 0) ? string.Empty : dval.ToString(LocalizationHelper.CurrentCulture);
        }

        public double? CollisionEnergy
        {
            get { return NullForEmpty(textCollisionEnergy.Text); }
            set { textCollisionEnergy.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? DeclusteringPotential
        {
            get { return NullForEmpty(textDeclusteringPotential.Text); }
            set { textDeclusteringPotential.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? CompensationVoltage
        {
            get { return NullForEmpty(textCompensationVoltage.Text); }
            set { textCompensationVoltage.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? SLens
        {
            get { return NullForEmpty(textSLens.Text); }
            set { textSLens.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? ConeVoltage
        {
            get { return NullForEmpty(textConeVoltage.Text); }
            set { textConeVoltage.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? RetentionTime
        {
            get { return NullForEmpty(textRetentionTime.Text); }
            set { textRetentionTime.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? RetentionTimeWindow
        {
            get { return NullForEmpty(textRetentionTimeWindow.Text); }
            set { textRetentionTimeWindow.Text = EmptyForNullOrNonPositive(value); }
        }

        public ExplicitRetentionTimeInfo ExplicitRetentionTimeInfo
        {
            get
            {
                return RetentionTime.HasValue
                    ? new ExplicitRetentionTimeInfo(RetentionTime.Value, RetentionTimeWindow)
                    : null;
            }
        }

        public double? IonMobility
        {
            get { return NullForEmpty(textIonMobility.Text); }
            set { textIonMobility.Text = EmptyForNullOrNonPositive(value); }
        }

        public double? IonMobilityHighEnergyOffset
        {
            get { return NullForEmpty(textIonMobilityHighEnergyOffset.Text); }
            set
            {
                textIonMobilityHighEnergyOffset.Text = value == null
                    ? string.Empty
                    : value.Value.ToString(LocalizationHelper.CurrentCulture);
            } // Negative values are normal here
        }

        public MsDataFileImpl.eIonMobilityUnits IonMobilityUnits
        {
            get
            {
                return comboBoxIonMobilityUnits.SelectedIndex >= 0
                    ? (MsDataFileImpl.eIonMobilityUnits) comboBoxIonMobilityUnits.SelectedIndex
                    : MsDataFileImpl.eIonMobilityUnits.none;
            }
            set { comboBoxIonMobilityUnits.SelectedIndex = (int) value; }
        }

        public double? CollisionalCrossSectionSqA
        {
            get { return NullForEmpty(textBoxCCS.Text); }
            set { textBoxCCS.Text = EmptyForNullOrNonPositive(value); }
        }

        public IsotopeLabelType IsotopeLabelType
        {
            get { return (_driverLabelType == null) ? null : _driverLabelType.SelectedMods.LabelType; }
            set
            {
                if (_driverLabelType != null) _driverLabelType.SelectedName = value.Name;
            }
        }

        public void OkDialog()
        {
            var helper = new MessageBoxHelper(this);
            var charge = 0;
            if (textCharge.Visible &&
                !helper.ValidateSignedNumberTextBox(textCharge, _minCharge, _maxCharge, out charge))
                return;
            var adduct = Adduct.NonProteomicProtonatedFromCharge(charge);
            if (RetentionTimeWindow.HasValue && !RetentionTime.HasValue)
            {
                helper.ShowTextBoxError(textRetentionTimeWindow,
                    Resources
                        .Peptide_ExplicitRetentionTimeWindow_Explicit_retention_time_window_requires_an_explicit_retention_time_value_);
                return;
            }
            if (Adduct.IsEmpty || Adduct.AdductCharge != adduct.AdductCharge)
                Adduct =
                    adduct; // Note: order matters here, this settor indirectly updates _formulaBox.MonoMass when formula is empty
            if (string.IsNullOrEmpty(_formulaBox.NeutralFormula))
            {
                // Can the text fields be understood as mz?
                if (!_formulaBox.ValidateAverageText(helper))
                    return;
                if (!_formulaBox.ValidateMonoText(helper))
                    return;
            }
            var monoMass = new TypedMass(_formulaBox.MonoMass ?? 0, MassType.Monoisotopic);
            var averageMass = new TypedMass(_formulaBox.AverageMass ?? 0, MassType.Average);
            if (monoMass < CustomMolecule.MIN_MASS || averageMass < CustomMolecule.MIN_MASS)
            {
                _formulaBox.ShowTextBoxErrorFormula(helper,
                    string.Format(
                        Resources
                            .EditCustomMoleculeDlg_OkDialog_Custom_molecules_must_have_a_mass_greater_than_or_equal_to__0__,
                        CustomMolecule.MIN_MASS));
                return;
            }
            if (monoMass > CustomMolecule.MAX_MASS || averageMass > CustomMolecule.MAX_MASS)
            {
                _formulaBox.ShowTextBoxErrorFormula(helper,
                    string.Format(
                        Resources
                            .EditCustomMoleculeDlg_OkDialog_Custom_molecules_must_have_a_mass_less_than_or_equal_to__0__,
                        CustomMolecule.MAX_MASS));
                return;
            }

            if ((_transitionSettings != null) &&
                (!_transitionSettings.IsMeasurablePrecursor(
                     adduct.MzFromNeutralMass(monoMass, MassType.Monoisotopic)) ||
                 !_transitionSettings.IsMeasurablePrecursor(adduct.MzFromNeutralMass(averageMass, MassType.Average))))
            {
                _formulaBox.ShowTextBoxErrorFormula(helper,
                    Resources
                        .SkylineWindow_AddMolecule_The_precursor_m_z_for_this_molecule_is_out_of_range_for_your_instrument_settings_);
                return;
            }
            if (!string.IsNullOrEmpty(_formulaBox.NeutralFormula))
            {
                try
                {
                    var name = textName.Text;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = _formulaBox.NeutralFormula; // Clip off any adduct description
                    }
                    SetResult(new CustomMolecule(_formulaBox.NeutralFormula, name), Adduct);
                }
                catch (InvalidDataException x)
                {
                    _formulaBox.ShowTextBoxErrorFormula(helper, x.Message);
                    return;
                }
            }
            else
            {
                SetResult(new CustomMolecule(monoMass, averageMass, textName.Text), Adduct);
            }
            // Did user change the list of heavy labels?
            if (_driverLabelType != null)
            {
                PeptideModifications modifications = new PeptideModifications(
                    _peptideSettings.Modifications.StaticModifications,
                    _peptideSettings.Modifications.MaxVariableMods,
                    _peptideSettings.Modifications.MaxNeutralLosses,
                    _driverLabelType.GetHeavyModifications(), // This is the only thing the user may have altered
                    _peptideSettings.Modifications.InternalStandardTypes);
                var settings = _peptideSettings.ChangeModifications(modifications);
                // Only update if anything changed
                if (!Equals(settings, _peptideSettings))
                {
                    SrmSettings newSettings = _parent.DocumentUI.Settings.ChangePeptideSettings(settings);
                    if (!_parent.ChangeSettings(newSettings, true))
                    {
                        return;
                    }
                    _peptideSettings = newSettings.PeptideSettings;
                }
            }

            // See if this combination of charge and label would conflict with any existing transition groups
            if (_existingIds != null && _existingIds.Any(t =>
            {
                var transitionGroup = t as TransitionGroup;
                return transitionGroup != null && Equals(transitionGroup.LabelType, IsotopeLabelType) &&
                       Equals(transitionGroup.PrecursorAdduct.AsFormula(),
                           Adduct
                               .AsFormula()) && // Compare AsFormula so proteomic and non-proteomic protonation are seen as same thing
                       !ReferenceEquals(t, _initialId);
            }))
            {
                helper.ShowTextBoxError(textName,
                    Resources
                        .EditCustomMoleculeDlg_OkDialog_A_precursor_with_that_adduct_and_label_type_already_exists_,
                    textName.Text);
                return;
            }

            // See if this would conflict with any existing transitions
            if (_existingIds != null && (_existingIds.Any(t =>
            {
                var transition = t as Transition;
                return transition != null && (Equals(transition.Adduct.AsFormula(), Adduct.AsFormula()) &&
                                              Equals(transition.CustomIon, ResultCustomMolecule)) &&
                       !ReferenceEquals(t, _initialId);
            })))
            {
                helper.ShowTextBoxError(textName,
                    Resources.EditCustomMoleculeDlg_OkDialog_A_similar_transition_already_exists_, textName.Text);
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void SetNameAndFormulaBoxText()
        {
            if (ResultCustomMolecule == null)
            {
                _formulaBox.Formula = string.Empty;
                _formulaBox.AverageMass = null;
                _formulaBox.MonoMass = null;
                textName.Text = string.Empty;
            }
            else
            {
                textName.Text = ResultCustomMolecule.Name ?? string.Empty;
                var displayFormula = ResultCustomMolecule.Formula ?? string.Empty;
                _formulaBox.Formula = displayFormula + (ResultAdduct.IsEmpty || ResultAdduct.IsProteomic
                                          ? string.Empty
                                          : ResultAdduct.AdductFormula);
                if (ResultCustomMolecule.Formula == null)
                {
                    _formulaBox.AverageMass = ResultCustomMolecule.AverageMass;
                    _formulaBox.MonoMass = ResultCustomMolecule.MonoisotopicMass;
                }
            }
        }

        private void textCharge_TextChanged(object sender, EventArgs e)
        {
            var helper = new MessageBoxHelper(this, false);
            int charge;
            if (!helper.ValidateSignedNumberTextBox(textCharge, _minCharge, _maxCharge, out charge))
            {
                return; // Not yet clear what the user has in mind
            }
            if (Adduct.IsEmpty || Adduct.AdductCharge != charge)
            {
                Adduct =
                    Adduct
                        .ChangeCharge(
                            charge); // Update the adduct with this new charge - eg for new charge 2, [M+Na] -> [M+2Na] 
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        private void comboLabelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Handle label type selection events, like <Edit list...>
            if (_driverLabelType != null)
            {
                _driverLabelType.SelectedIndexChangedEvent();
                if (_driverLabelType.SelectedMods.Modifications.Any(m => m.LabelAtoms != LabelAtoms.None))
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var m in _driverLabelType.SelectedMods.Modifications.Where(
                        m => m.LabelAtoms != LabelAtoms.None))
                    {
                        foreach (var l in m.LabelNames)
                        {
                            if (!dict.ContainsKey(l))
                            {
                                dict.Add(BioMassCalc.MONOISOTOPIC.StripLabelsFromFormula(l), l);
                            }
                        }
                    }
                    FormulaBox.IsotopeLabelsForMassCalc = dict;
                }
                else
                {
                    FormulaBox.IsotopeLabelsForMassCalc = null;
                }
            }
        }


        #region For Testing

        public String NameText
        {
            get { return textName.Text; }
            set { textName.Text = value; }
        }

        public FormulaBox FormulaBox
        {
            get { return _formulaBox; }
        }

        #endregion
    }
}