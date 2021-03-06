﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public enum EnumDecisionRule
    {
        Predetermined = 1,      // always one or off
        Periodic = 2,           // employ at certain frequency
        ConditionBased = 3,     // employ when a condition is met
        IntervalBased = 4,      // employ during a certain time interval
        Dynamic = 5             // guided by a dynamic policy 
    }

    public abstract class DecisionRule
    {
        protected int _defaultSwitchValue;
        public DecisionRule(int defaultSwtichValue)
        {
            _defaultSwitchValue = defaultSwtichValue;
        }

        public virtual int GetSwitchStatus(int currentSwitchStatus, int epiTimeIndex) { return 0; }
    }

    // predetermined decision rule 
    public class DecionRule_Predetermined : DecisionRule
    {
        private int _switchValue = 0;

        public DecionRule_Predetermined(int predeterminedSwitchValue): base(predeterminedSwitchValue)
        {
            _switchValue = predeterminedSwitchValue;
        }

        public override int GetSwitchStatus(int currentSwitchStatus, int epiTimeIndex)
        {
            return _switchValue;
        }
    }

    // condition-based decision rule
    public class DecisionRule_ConditionBased : DecisionRule
    {
        private List<Condition> _conditions;
        private int _conditionIDToTurnOn;
        private int _conditionIDToTurnOff;

        public DecisionRule_ConditionBased(
            List<Condition> conditions, int conditionIDToTurnOn, int conditionIDToTurnOff, int defaultSwtichValue): base (defaultSwtichValue)
        {
            _conditions = conditions;
            _conditionIDToTurnOn = conditionIDToTurnOn;
            _conditionIDToTurnOff = conditionIDToTurnOff;
        }

        public override int GetSwitchStatus(int currentSwitchStatus, int epiTimeIndex)
        {
            int value = currentSwitchStatus;
            // if to turn on
            if (currentSwitchStatus == 0)
            {
                if (_conditions[_conditionIDToTurnOn].Value.HasValue)
                    value = _conditions[_conditionIDToTurnOn].Value.Value ? 1 : 0;
                else
                    value = _defaultSwitchValue;
            }
            else if (currentSwitchStatus == 1)
            {
                if (_conditions[_conditionIDToTurnOff].Value.HasValue)
                    value = _conditions[_conditionIDToTurnOff].Value.Value ? 0 : 1;
                else
                    value = _defaultSwitchValue;
            }

            return value;
        }
    }

    // periodic decision rule 
    public class DecionRule_Periodic : DecisionRule
    {
        private int _frequency_nOfDcisionPeriods = 0;
        private int _duration_nOfDcisionPeriods = 0;

        public DecionRule_Periodic(int frequency_nOfDcisionPeriods, int duration_nOfDcisionPeriods, int defaultSwtichValue): base(defaultSwtichValue)
        {
            _frequency_nOfDcisionPeriods = frequency_nOfDcisionPeriods;
            _duration_nOfDcisionPeriods = duration_nOfDcisionPeriods;
        }
    }

    // interval-based decision rule 
    public class DecionRule_IntervalBased : DecisionRule
    {
        private int _timeIndexToTurnOn;
        private int _timeIndexToTurnOff;

        public DecionRule_IntervalBased(int timeIndexToTurnOn, int timeIndexToTurnOff, int defaultSwtichValue): base(defaultSwtichValue)
        {
            _timeIndexToTurnOn = timeIndexToTurnOn;
            _timeIndexToTurnOff = timeIndexToTurnOff;
        }
    }

    // dynamic decision rule 
    public class DecionRule_Dynamic : DecisionRule
    {
        public DecionRule_Dynamic(int defaultSwtichValue): base (defaultSwtichValue)
        {
        }
    }

    public class IntervalBasedStaticPolicy
    {
        int _id;
        int _interventionCombinationCode;
        double[] _timeToUseInterventions;
        int[] _numOfDecisionPointsToUseInterventions;

        public IntervalBasedStaticPolicy(int id, int interventionCombinationCode, double[] timeToUseInterventions, int[] numOfDecisionPointsToUseInterventions)
        {
            _id = id;
            _interventionCombinationCode = interventionCombinationCode;
            _timeToUseInterventions = (double[])timeToUseInterventions.Clone();
            _numOfDecisionPointsToUseInterventions = (int[])numOfDecisionPointsToUseInterventions.Clone();
        }
        public int ID
        {
            get { return _id; }
        }
        public int InterventionCombinationCode
        {
            get { return _interventionCombinationCode; }
        }
        public double[] TimeToUseInterventions
        {
            get { return _timeToUseInterventions; }
        }
        public int[] NumOfDecisionPointsToUseInterventions
        {
            get { return _numOfDecisionPointsToUseInterventions; }
        }
    }



}
