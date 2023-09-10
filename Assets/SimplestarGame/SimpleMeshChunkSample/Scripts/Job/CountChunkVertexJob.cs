using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SimplestarGame
{
    /// <summary>
    /// Chunk結合したメッシュの総頂点数を予め調べる処理
    /// バッファを固定長で確保してから、そのバッファにデータを書き込んでいく処理がその先に待つ
    /// 今回は InsetCube のため、+X と -Y, -Z 方向の面側のカリングをしない判定となっている
    /// </summary>
    [BurstCompile]
    public struct CountChunkVertexJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<XYZ> xyz;
        [ReadOnly] public NativeArray<int> vertexCounts;
        [ReadOnly] public NativeArray<byte> voxelData;
        public NativeArray<int> results;
        public int heightDepth;
        public int height;
        public int depth;

        /// <summary>
        /// ボクセルの x, y, z 座標指定での値取得
        /// </summary>
        /// <param name="voxelData">ボクセルデータ</param>
        /// <param name="x">幅座標</param>
        /// <param name="y">高さ座標</param>
        /// <param name="z">奥行き座標</param>
        /// <returns></returns>
        byte GetVoxelFlag(ref NativeArray<byte> voxelData, int x, int y, int z)
        {
            return voxelData[x * this.heightDepth + y * this.depth + z];
        }

        public void Execute(int index)
        {
            var xyz = this.xyz[index];
            var x = xyz.x;
            var y = xyz.y;
            var z = xyz.z;

            if (this.GetVoxelFlag(ref voxelData, x, y, z) == 255)
            {
                for (int c = 0; c < this.vertexCounts.Length; c++)
                {
                    var count = this.vertexCounts[c];
                    if (c == CAWFile.PLUS_X)
                    {
                        this.results[index] += count;
                    }
                    else if (c == CAWFile.PLUS_Y)
                    {
                        if (y + 1 != this.height)
                        {
                            if (this.GetVoxelFlag(ref voxelData, x, y + 1, z) != 255)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.PLUS_Z)
                    {
                        if (z + 1 != this.depth)
                        {
                            if (this.GetVoxelFlag(ref voxelData, x, y, z + 1) != 255)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.MINUS_X)
                    {
                        if (x != 0)
                        {
                            if (this.GetVoxelFlag(ref voxelData, x - 1, y, z) != 255)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.MINUS_Y)
                    {
                        this.results[index] += count;
                    }
                    else if (c == CAWFile.MINUS_Z)
                    {
                        this.results[index] += count;
                    }
                    else if (c == CAWFile.REMAIN)
                    {
                        this.results[index] += count;
                    }
                }
            }
        }
    }
}