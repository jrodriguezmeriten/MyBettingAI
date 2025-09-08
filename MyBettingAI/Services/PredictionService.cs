using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyBettingAI.Services
{
    /// <summary>
    /// Service for training and predicting football match outcomes.
    /// Servicio para entrenar y predecir resultados de partidos de fútbol.
    /// </summary>
    public class PredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer _trainedModel;

        /// <summary>
        /// Constructor initializes the ML.NET context.
        /// Constructor que inicializa el contexto de ML.NET.
        /// </summary>
        public PredictionService()
        {
            _mlContext = new MLContext(seed: 0);
        }

        #region Training Methods

        /// <summary>
        /// Trains a multiclass classification model using the provided training data.
        /// Entrena un modelo de clasificación multiclase usando los datos de entrenamiento proporcionados.
        /// </summary>
        /// <param name="trainingData">List of training features / Lista de características de entrenamiento</param>
        /// <param name="algorithm">Algorithm to use (SDCA or LBFGS) / Algoritmo a usar (SDCA o LBFGS)</param>
        /// <returns>Tuple containing accuracy and log loss / Tupla con precisión y log loss</returns>
        public (double Accuracy, double LogLoss) TrainModel(List<TrainingFeatures> trainingData, string algorithm = "SDCA")
        {
            if (trainingData == null || trainingData.Count < 50)
            {
                Console.WriteLine("Warning: Not enough data to train the model / No hay suficientes datos para entrenar el modelo");
                return (0, 0);
            }

            try
            {
                // Convert training data to IDataView
                // Convertir datos de entrenamiento a IDataView
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Data processing pipeline: map label, concatenate features, normalize
                // Pipeline de procesamiento de datos: mapear label, concatenar features, normalizar
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

                // Fit and transform the preprocessing pipeline
                // Ajustar y transformar el pipeline de preprocesamiento
                var preprocessedData = dataProcessPipeline.Fit(dataView).Transform(dataView);

                // Train model depending on the selected algorithm
                // Entrenar modelo según el algoritmo seleccionado
                ITransformer model;

                switch (algorithm.ToLower())
                {
                    case "lbfgs":
                        model = _mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy()
                            .Fit(preprocessedData);
                        Console.WriteLine("Using algorithm: LBFGS / Usando algoritmo: LBFGS");
                        break;

                    case "sdca":
                    default:
                        model = _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy()
                            .Fit(preprocessedData);
                        Console.WriteLine("Using algorithm: SDCA / Usando algoritmo: SDCA");
                        break;
                }

                // Create final pipeline with post-processing
                // Crear pipeline final con post-procesamiento
                var postProcessPipeline = _mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel");
                _trainedModel = postProcessPipeline.Fit(model.Transform(preprocessedData));

                Console.WriteLine($"Model trained with {trainingData.Count} matches / Modelo entrenado con {trainingData.Count} partidos");

                // Evaluate model
                // Evaluar modelo
                var predictions = _trainedModel.Transform(dataView);
                var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

                Console.WriteLine($"Accuracy / Precisión: {metrics.MicroAccuracy:P2}");
                Console.WriteLine($"Log Loss: {metrics.LogLoss:F4}");

                return (metrics.MicroAccuracy, metrics.LogLoss);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error training model: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Tests different algorithms and compares results.
        /// Prueba diferentes algoritmos y compara los resultados.
        /// </summary>
        public void TestDifferentAlgorithms(List<TrainingFeatures> trainingData)
        {
            Console.WriteLine("Testing different algorithms / Probando diferentes algoritmos");

            var algorithms = new[] { "SDCA", "LBFGS" };
            var results = new Dictionary<string, (double Accuracy, double LogLoss)>();

            foreach (var algo in algorithms)
            {
                Console.WriteLine($"\n--- Testing {algo} ---");
                var testModel = new PredictionService();
                var metrics = testModel.TrainModel(trainingData, algo);
                results[algo] = metrics;
            }

            // Show comparative results
            // Mostrar resultados comparativos
            Console.WriteLine("\nResults / Resultados:");
            foreach (var result in results.OrderByDescending(r => r.Value.Accuracy))
            {
                Console.WriteLine($"{result.Key}: Accuracy={result.Value.Accuracy:P2}, LogLoss={result.Value.LogLoss:F4}");
            }
        }

        /// <summary>
        /// Finds the best algorithm based on accuracy and log loss.
        /// Encuentra el mejor algoritmo basado en precisión y log loss.
        /// </summary>
        public string FindBestAlgorithm(List<TrainingFeatures> trainingData)
        {
            var algorithms = new[] { "SDCA", "LBFGS" };
            var results = new Dictionary<string, (double Accuracy, double LogLoss)>();

            foreach (var algo in algorithms)
            {
                var testModel = new PredictionService();
                var metrics = testModel.TrainModel(trainingData, algo);
                results[algo] = metrics;
            }

            var bestAlgorithm = results.OrderByDescending(r => r.Value.Accuracy)
                                       .ThenBy(r => r.Value.LogLoss)
                                       .First().Key;

            Console.WriteLine($"Best algorithm found: {bestAlgorithm} / Mejor algoritmo encontrado: {bestAlgorithm}");
            return bestAlgorithm;
        }

        #endregion

        #region Prediction Methods

        /// <summary>
        /// Predicts probabilities for a match using trained model.
        /// Predice probabilidades para un partido usando el modelo entrenado.
        /// </summary>
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

        /// <summary>
        /// Simple prediction with default parameters for debugging.
        /// Predicción simple con parámetros por defecto para depuración.
        /// </summary>
        public (double HomeWin, double Draw, double AwayWin) PredictSimple(float homeStrength, float awayStrength)
        {
            if (_trainedModel == null)
            {
                Console.WriteLine("Model not trained - returning default values / Modelo no entrenado - devolviendo valores por defecto");
                return (0.33, 0.33, 0.33);
            }

            try
            {
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

                var predictionEngine = _mlContext.Model.CreatePredictionEngine<TrainingFeatures, MatchPrediction>(_trainedModel);
                var prediction = predictionEngine.Predict(features);

                return (prediction.HomeWinProbability, prediction.DrawProbability, prediction.AwayWinProbability);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in prediction: {ex.Message}");
                return (0.33, 0.33, 0.33);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents prediction results for a single match.
    /// Representa los resultados de predicción de un partido.
    /// </summary>
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

    /// <summary>
    /// Wrapper class for predicted label.
    /// Clase wrapper para la etiqueta predicha.
    /// </summary>
    public class PredictionResult
    {
        public string PredictedLabel { get; set; }
    }
}
