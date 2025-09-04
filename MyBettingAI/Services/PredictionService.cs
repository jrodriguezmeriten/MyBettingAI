using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Generic;
using System.Linq;

namespace MyBettingAI.Services
{
    public class PredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer _trainedModel;

        public PredictionService()
        {
            _mlContext = new MLContext(seed: 0);
        }

        public void TrainModel(List<TrainingFeatures> trainingData)
        {
            if (trainingData == null || trainingData.Count < 30)
            {
                Console.WriteLine("⚠️ No hay suficientes datos para entrenar el modelo");
                return;
            }

            // Convertir datos a IDataView
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Pipeline de preprocesamiento
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(_mlContext.Transforms.Concatenate("Features",
                    nameof(TrainingFeatures.HomeTeamStrength),
                    nameof(TrainingFeatures.AwayTeamStrength),
                    nameof(TrainingFeatures.HomeHomeWins),
                    nameof(TrainingFeatures.AwayAwayWins),
                    nameof(TrainingFeatures.AvgHomeGoals),
                    nameof(TrainingFeatures.AvgAwayGoals)))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // Entrenar el modelo
            _trainedModel = pipeline.Fit(dataView);

            Console.WriteLine($"✅ Modelo entrenado con {trainingData.Count} partidos");

            // Evaluar el modelo (opcional)
            var predictions = _trainedModel.Transform(dataView);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);
            Console.WriteLine($"📊 Precisión del modelo: {metrics.MacroAccuracy:P2}");
        }

        public (double HomeWin, double Draw, double AwayWin) PredictMatch(float homeStrength, float awayStrength, float homeWins, float awayWins, float avgHomeGoals, float avgAwayGoals)
        {
            if (_trainedModel == null)
                return (0.33, 0.33, 0.33); // Probabilidades uniformes si no hay modelo

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<TrainingFeatures, MatchPrediction>(_trainedModel);

            var features = new TrainingFeatures
            {
                HomeTeamStrength = homeStrength,
                AwayTeamStrength = awayStrength,
                HomeHomeWins = homeWins,
                AwayAwayWins = awayWins,
                AvgHomeGoals = avgHomeGoals,
                AvgAwayGoals = avgAwayGoals
            };

            var prediction = predictionEngine.Predict(features);
            return (prediction.HomeWinProbability, prediction.DrawProbability, prediction.AwayWinProbability);
        }
    }

    public class MatchPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedResult { get; set; }

        public float[] Score { get; set; }

        public double HomeWinProbability => Softmax(0);
        public double DrawProbability => Softmax(1);
        public double AwayWinProbability => Softmax(2);

        private double Softmax(int index)
        {
            if (Score == null || Score.Length < 3)
                return 0.33;

            var expScores = Score.Select(s => (double)Math.Exp(s)).ToArray();
            var sumExpScores = expScores.Sum();
            return Math.Exp(Score[index]) / sumExpScores;
        }
    }
}