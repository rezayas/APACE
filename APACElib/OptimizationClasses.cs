﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib
{
    public class GonorrheaEpiModeller : SimModel
    {
        const double PENALTY = 10e8;
        private double _wtp = 0;

        public EpidemicModeller EpiModeller { get; private set; }

        public GonorrheaEpiModeller(EpidemicModeller epiModeller, double wtp)
        {
            EpiModeller = epiModeller;
            _wtp = wtp;
        }

        /// <param name="x"> x[0]: threshold to switch, x[1]: change in prevalence to switch  </param>
        /// <returns></returns>
        public override double GetAReplication(Vector<double> x, bool ifResampleSeeds)
        {
            double objValue = 0;

            // make sure variables are in feasible range; if not, add the penalty to the objective function
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] < 0)
                {
                    objValue += PENALTY * Math.Pow(x[i], 2);
                    x[i] = 0;
                }
                else if (x[i] > 1)
                {
                    objValue += PENALTY * Math.Pow(x[i]-1, 2);
                    x[i] = 1;
                } 
            }
            if (x[0] < x[1])
            {
                objValue += PENALTY * Math.Pow(x[1] - x[0], 2);
                x[1] = x[0];
            }

            // update the thresholds in the epidemic modeller
            foreach (Epidemic epi in EpiModeller.Epidemics)
            {
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx ++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx]).UpdateThresholds(x.ToArray());
            }

            // simulate
            EpiModeller.SimulateEpidemics(ifResampleSeeds);

            // calcualte net monetary benefit
            objValue += _wtp* EpiModeller.SimSummary.DALYStat.Mean + EpiModeller.SimSummary.CostStat.Mean;
                       
            return objValue;
        }
    }

    public class OptimizeGonohrrea
    {
        const int NUM_OF_VARIABLES = 2;

        public List<double[]> Summary = new List<double[]>();

        public void Run(EpidemicModeller epiModeller, ModelSettings modelSets)
        {
            // initial thresholds for the initial WTP 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // for all wtp values
            for (double wtp = modelSets.OptmzSets.WTP_min; 
                wtp <= modelSets.OptmzSets.WTP_max; 
                wtp += modelSets.OptmzSets.WTP_step)
            {

                // find the a value that minimizes f
                double fStar = double.MaxValue;
                Vector<double> xStar = Vector<double>.Build.Dense(x0.Count);
                double aStar = 0;

                foreach (double a in modelSets.OptmzSets.StepSize_as)
                {
                    // create a stochastic approximation object
                    StochasticApproximation optimizer = new StochasticApproximation(
                        simModel: new GonorrheaEpiModeller(epiModeller, wtp),
                        derivativeStep: modelSets.OptmzSets.DerivativeStep,
                        stepSize: new StepSize(a: a)
                        );

                    // minimize
                    optimizer.Minimize(
                        maxItrs: modelSets.OptmzSets.NOfItrs,
                        lastItrsToAve: modelSets.OptmzSets.NOfLastItrsToAverage,
                        x0: x0);

                    // export results
                    if (modelSets.OptmzSets.IfExportResults)
                        optimizer.ExportResultsToCSV("wtp" + wtp + "-a" + a + ".csv");
                    
                    // if this a led to the minimum f
                    if (optimizer.fStar < fStar)
                    {
                        fStar = optimizer.fStar;
                        xStar = optimizer.xStar;
                        aStar = a;
                    }
                }

                // use this xStar as the intial variable for the next wtp
                x0 = xStar;

                // store results
                double[] result = new double[3 + NUM_OF_VARIABLES]; // 1 for wtp, 1 for fStar, 1 for a0
                result[0] = wtp;
                result[1] = aStar;
                result[2] = fStar;
                result[3] = xStar[0];
                result[4] = xStar[1];
                Summary.Add(result);
            }
        }

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_VARIABLES + 1];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_VARIABLES + 3; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

}
