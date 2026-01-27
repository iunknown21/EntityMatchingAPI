using System;

namespace EntityMatching.Core.Utilities
{
    /// <summary>
    /// Static utility class for vector operations used in similarity calculations
    /// Optimized for 1536-dimensional vectors (OpenAI text-embedding-3-small)
    /// </summary>
    public static class VectorMath
    {
        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// Returns a value between -1 (opposite) and 1 (identical)
        /// Returns 0 for orthogonal (unrelated) vectors
        /// </summary>
        /// <param name="vector1">First vector</param>
        /// <param name="vector2">Second vector</param>
        /// <returns>Cosine similarity score (0 to 1 for normalized vectors)</returns>
        public static float CosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null)
                throw new ArgumentNullException("Vectors cannot be null");

            if (vector1.Length != vector2.Length)
                throw new ArgumentException($"Vectors must have same dimensions. Got {vector1.Length} and {vector2.Length}");

            if (vector1.Length == 0)
                throw new ArgumentException("Vectors cannot be empty");

            float dotProduct = 0f;
            float magnitude1 = 0f;
            float magnitude2 = 0f;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = MathF.Sqrt(magnitude1);
            magnitude2 = MathF.Sqrt(magnitude2);

            // Prevent divide by zero
            if (magnitude1 == 0f || magnitude2 == 0f)
                return 0f;

            return dotProduct / (magnitude1 * magnitude2);
        }

        /// <summary>
        /// Calculate magnitude (L2 norm) of a vector
        /// </summary>
        /// <param name="vector">Input vector</param>
        /// <returns>Magnitude of the vector</returns>
        public static float Magnitude(float[] vector)
        {
            if (vector == null || vector.Length == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < vector.Length; i++)
            {
                sum += vector[i] * vector[i];
            }
            return MathF.Sqrt(sum);
        }

        /// <summary>
        /// Normalize a vector to unit length (magnitude = 1)
        /// </summary>
        /// <param name="vector">Input vector</param>
        /// <returns>Normalized vector (unit length)</returns>
        public static float[] Normalize(float[] vector)
        {
            if (vector == null)
                throw new ArgumentNullException(nameof(vector));

            var mag = Magnitude(vector);
            if (mag == 0f)
                return vector; // Return original if zero vector

            var normalized = new float[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                normalized[i] = vector[i] / mag;
            }
            return normalized;
        }

        /// <summary>
        /// Calculate dot product of two vectors
        /// </summary>
        /// <param name="vector1">First vector</param>
        /// <param name="vector2">Second vector</param>
        /// <returns>Dot product</returns>
        public static float DotProduct(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null)
                throw new ArgumentNullException("Vectors cannot be null");

            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have same dimensions");

            float sum = 0f;
            for (int i = 0; i < vector1.Length; i++)
            {
                sum += vector1[i] * vector2[i];
            }
            return sum;
        }
    }
}
