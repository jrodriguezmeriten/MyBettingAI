using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
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

        public (double Accuracy, double LogLoss) TrainModel(List<TrainingFeatures> trainingData, string algorithm = "SDCA")
        {
            if (trainingData == null || trainingData.Count < 50)
            {
                Console.WriteLine("⚠️ No hay suficientes datos para entrenar el modelo");
                return (0, 0);
            }

            try
            {
                // Convertir datos a IDataView
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Construir el pipeline en pasos separados para evitar problemas de tipos
                var dataProcessPipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        nameof(TrainingFeatures.HomeTeamStrength),
                        nameof(TrainingFeatures.AwayTeamStrength),
                        nameof(TrainingFeatures.HomeForm),
                        nameof(TrainingFeatures.AwayForm),
                        nameof(TrainingFeatures.HomeAttackStrength),
                        nameof(TrainingFeatures.AwayAttackStrength),
                        nameof(TrainingFeatures.HomeDefenseWeakness),
                        nameof(TrainingFeatures.AwayDefenseWeakness),
                        nameof(TrainingFeatures.HistoricalDraws)))
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features"));

                // Aplicar el preprocesamiento
                var preprocessedData = dataProcessPipeline.Fit(dataView).Transform(dataView);

                // Entrenar el modelo según el algoritmo seleccionado
                ITransformer model;

                switch (algorithm.ToLower())
                {
                    case "lbfgs":
                        model = _mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy()
                            .Fit(preprocessedData);
                        Console.WriteLine("🎯 Usando algoritmo: LBFGS");
                        break;

                    case "sdca":
                    default:
                        model = _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy()
                            .Fit(preprocessedData);
                        Console.WriteLine("🎯 Usando algoritmo: SDCA");
                        break;
                }

                // Crear el pipeline final con post-procesamiento
                var postProcessPipeline = _mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel");
                _trainedModel = postProcessPipeline.Fit(model.Transform(preprocessedData));

                Console.WriteLine($"✅ Modelo entrenado con {trainingData.Count} partidos");

                // Evaluar el modelo
                var predictions = _trainedModel.Transform(dataView);
                var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

                Console.WriteLine($"📊 Precisión: {metrics.MicroAccuracy:P2}");
                Console.WriteLine($"📊 Log Loss: {metrics.LogLoss:F4}");

                return (metrics.MicroAccuracy, metrics.LogLoss);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error entrenando modelo: {ex.Message}");
                return (0, 0);
            }
        }

        public void TestDifferentAlgorithms(List<TrainingFeatures> trainingData)
        {
            Console.WriteLine("🧪 PROBANDO DIFERENTES ALGORITMOS:");

            // Solo algoritmos que sabemos que funcionan
            var algorithms = new[] { "SDCA", "LBFGS" };
            var results = new Dictionary<string, (double Accuracy, double LogLoss)>();

            foreach (var algo in algorithms)
            {
                Console.WriteLine($"\n--- Probando {algo} ---");

                // Crear una nueva instancia para cada prueba
                var testModel = new PredictionService();
                var metrics = testModel.TrainModel(trainingData, algo);

                results[algo] = metrics;
            }

            // Mostrar resultados comparativos
            Console.WriteLine("\n📈 RESULTADOS COMPARATIVOS:");
            foreach (var result in results.OrderByDescending(r => r.Value.Accuracy))
            {
                Console.WriteLine($"   {result.Key}: Precisión = {result.Value.Accuracy:P2}, Log Loss = {result.Value.LogLoss:F4}");
            }
        }

        public string FindBestAlgorithm(List<TrainingFeatures> trainingData)
        {
            Console.WriteLine("🔍 BUSCANDO MEJOR ALGORITMO...");

            var algorithms = new[] { "SDCA", "LBFGS" };
            var results = new Dictionary<string, (double Accuracy, double LogLoss)>();

            foreach (var algo in algorithms)
            {
                var testModel = new PredictionService();
                var metrics = testModel.TrainModel(trainingData, algo);
                results[algo] = metrics;
            }

            // Seleccionar el mejor
            var bestAlgorithm = results.OrderByDescending(r => r.Value.Accuracy)
                                      .ThenBy(r => r.Value.LogLoss)
                                      .First().Key;

            Console.WriteLine($"✅ MEJOR ALGORITMO ENCONTRADO: {bestAlgorithm}");
            return bestAlgorithm;
        }

        public (double HomeWin, double Draw, double AwayWin) PredictMatch(
            float homeStrength, float awayStrength, float homeForm, float awayForm,
            float homeAttack, float awayAttack, float homeDefense, float awayDefense,
            float historicalDraws)
        {
            if (_trainedModel == null)
                return (0.33, 0.33, 0.33);

            var features = new TrainingFeatures
            {
                HomeTeamStrength = homeStrength,
                AwayTeamStrength = awayStrength,
                HomeForm = homeForm,
                AwayForm = awayForm,
                HomeAttackStrength = homeAttack,
                AwayAttackStrength = awayAttack,
                HomeDefenseWeakness = homeDefense,
                AwayDefenseWeakness = awayDefense,
                HistoricalDraws = historicalDraws
            };

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<TrainingFeatures, MatchPrediction>(_trainedModel);
            var prediction = predictionEngine.Predict(features);

            return (prediction.HomeWinProbability, prediction.DrawProbability, prediction.AwayWinProbability);
        }


        public (double HomeWin, double Draw, double AwayWin) PredictSimple(float homeStrength, float awayStrength)
        {
            if (_trainedModel == null)
            {
                Console.WriteLine("❌ Modelo no entrenado - devolviendo valores por defecto");
                return (0.33, 0.33, 0.33);
            }

            try
            {
                // Valores por defecto para debugging
                var features = new TrainingFeatures
                {
                    HomeTeamStrength = homeStrength,
                    AwayTeamStrength = awayStrength,
                    HomeForm = 3f,
                    AwayForm = 2f,
                    HomeAttackStrength = 1.8f,
                    AwayAttackStrength = 1.5f,
                    HomeDefenseWeakness = 1.2f,
                    AwayDefenseWeakness = 1.4f,
                    HistoricalDraws = 1f
                };

                Console.WriteLine($"🔍 Features para predicción: Home={homeStrength}, Away={awayStrength}");

                var predictionEngine = _mlContext.Model.CreatePredictionEngine<TrainingFeatures, MatchPrediction>(_trainedModel);
                var prediction = predictionEngine.Predict(features);

                Console.WriteLine($"🔮 Predicción: Home={prediction.HomeWinProbability:P2}, Draw={prediction.DrawProbability:P2}, Away={prediction.AwayWinProbability:P2}");

                return (prediction.HomeWinProbability, prediction.DrawProbability, prediction.AwayWinProbability);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en predicción: {ex.Message}");
                Console.WriteLine($"📋 StackTrace: {ex.StackTrace}");
                return (0.33, 0.33, 0.33);
            }
        }
    }

    public class PredictionResult
    {
        public string PredictedLabel { get; set; }
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