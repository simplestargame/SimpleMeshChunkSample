using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SimplestarGame
{
    /// <summary>
    /// 三次元ボクセルデータを作るジョブ
    /// パーリンノイズを使って、塊がなめらかに連続するような感じ
    /// </summary>
    [BurstCompile]
    struct VoxelGenerationJob : IJobParallelFor
    {
        public NativeArray<byte> VoxelData;
        public int Width;
        public int Height;
        public int Depth;
        public float DensityThreshold;

        public void Execute(int index)
        {
            int z = index / (this.Width * this.Height);
            int y = (index - z * this.Width * this.Height) / this.Width;
            int x = index - z * this.Width * this.Height - y * this.Width;

            float noiseValue = noise.cnoise(new float3(x * 0.1f, y * 0.1f + 1000f, z * 0.1f + 2000f));

            this.VoxelData[index] = (noiseValue > this.DensityThreshold) ? (byte)255 : (byte)0;
        }
    }
}
