﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public class Calibration
    {
        public int NumOfCalibratoinTargets { get; } = 0;
        public int NumOfDiscardedTrajs { get; set; } = 0;
        public double TimeUsed { get; set; } = 0;

        public void Reset()
        {

        }
    }

    public class SpecialStatCalibrInfo
    {
        public enum EnumMeasureOfFit : int
        {
            FeasibleRangeOnly = 1,
            Likelihood = 2,
            Fourier = 3,
        }
        public enum EnumLikelihoodFunc: int
        {
            NoSpecified = 0,
            Normal = 1,
            Binomial = 2,
            Multinomial = 3,
        }

        public EnumMeasureOfFit GoodnessOfFit { get; }
        public EnumLikelihoodFunc LikelihoodFunc { get; }
        public bool IfCheckWithinFeasibleRange { get; } = false;
        public double LowFeasibleRange { get; }
        public double UpFeasibleRange { get; }

        public SpecialStatCalibrInfo(
            string measureOfFit,
            string likelihoodFunction = "",
            bool ifCheckWithinFeasibleRange = false, 
            double lowFeasibleBound = 0, 
            double upFeasibleBound = double.MaxValue)
        {

            IfCheckWithinFeasibleRange = ifCheckWithinFeasibleRange;
            LowFeasibleRange = lowFeasibleBound;
            UpFeasibleRange = upFeasibleBound;

            switch (measureOfFit)
            {
                case "Feasible Range Only":
                    {
                        GoodnessOfFit = EnumMeasureOfFit.FeasibleRangeOnly;
                        if (!ifCheckWithinFeasibleRange)
                            throw new ArgumentException("Inconsistant setting.");                        
                    }
                    break;
                case "Likelihood":
                    {
                        GoodnessOfFit = EnumMeasureOfFit.Likelihood;
                        switch (likelihoodFunction)
                        {
                            case "Normal":
                                LikelihoodFunc = EnumLikelihoodFunc.Normal;
                                break;
                            case "Binomial":
                                LikelihoodFunc = EnumLikelihoodFunc.Binomial;
                                break;
                            case "Multinomial":
                                LikelihoodFunc = EnumLikelihoodFunc.Multinomial;
                                break;
                            default:
                                throw new ArgumentException("Likelihood function not defined.");
                        }
                    }
                    break;
                case "Fourier":
                    {
                        GoodnessOfFit = EnumMeasureOfFit.Fourier;
                    }
                    break;
                default:
                    throw new ArgumentException("Goodness-of-fit not defined.");
            }        
        }

    }



    public abstract class CalibrTimeSeriesData
    {

    }

    public class CalibrTS_Normal : CalibrTimeSeriesData
    {
        double[] _obs;
        double[] _measureError;

        public CalibrTS_Normal(double[] obs, double[] measureError)
        {
            _obs = obs;
            _measureError = measureError;
        }
    }

    public abstract class CalibrTS_Binomial: CalibrTimeSeriesData
    {

    }

    public class CalibrTS_Binomial_NSim : CalibrTS_Binomial
    {
        int _IDofN;
        double[] _k;

        public CalibrTS_Binomial_NSim(int idOfN, double[] k)
        {
            _IDofN = idOfN;
            _k = k;
        }
    }

    public class CalibrTS_Binomial_NData : CalibrTS_Binomial
    {
        int[] _N;
        double[] _p;

        public CalibrTS_Binomial_NData(int[] N, double[] p)
        {
            _N = N;
            _p = p;
        }

    }

}
