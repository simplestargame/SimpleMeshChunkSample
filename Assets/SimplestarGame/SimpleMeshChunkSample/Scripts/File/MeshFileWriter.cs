using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace SimplestarGame
{
    /// <summary>
    /// meshFilter の mesh をファイル出力します（Writeボタンを押すと StreamingAssets フォルダにファイルを出力する）
    /// ファイルフォーマットは 4byte でファイル識別、7つの int 値が x, y, z, -x, -y, -z, other の面構成頂点数を表し
    /// 後続は同頂点三角形リストデータとして、頂点データが羅列する
    /// </summary>
    public class MeshFileWriter : MonoBehaviour
    {
        [SerializeField] MeshFilter meshFilter;

        public void WriteMeshFile()
        {
            // まずはカスタム頂点レイアウトの頂点データを作成
            var customLayoutMesh = new CustomLayoutMesh(1);
            customLayoutMesh.SetMeshData(0, this.meshFilter.sharedMesh);
            customLayoutMesh.Schedule().Complete();
            var meshData = customLayoutMesh.GetMeshData(0);
            var xPV = new List<CustomVertexLayout>();
            var yPV = new List<CustomVertexLayout>();
            var zPV = new List<CustomVertexLayout>();
            var xNV = new List<CustomVertexLayout>();
            var yNV = new List<CustomVertexLayout>();
            var zNV = new List<CustomVertexLayout>();
            var __V = new List<CustomVertexLayout>();
            // その頂点データを解析して 7つの構成頂点三角形リストデータを作成
            ClassifyVertices(meshData, xPV, yPV, zPV, xNV, yNV, zNV, __V);
            {
                NativeArray<byte> fileData;
                // これをファイルデータとしてフォーマット化
                FormatFileData(xPV, yPV, zPV, xNV, yNV, zNV, __V, out fileData);
                // 作られた fileData をファイル出力
                SaveBytesToFile(fileData, Path.Combine(Application.streamingAssetsPath, $"{this.meshFilter.sharedMesh.name}.caw"));
                fileData.Dispose();
            }
        }

        /// <summary>
        /// 三角形ごとにループし、7つの頂点データ群に分類
        /// </summary>
        /// <param name="meshData">カスタム頂点レイアウトの頂点データ</param>
        /// <param name="xPV">+x方向を向く面を構成している頂点リスト</param>
        /// <param name="yPV">+y方向を向く面を構成している頂点リスト</param>
        /// <param name="zPV">+z方向を向く面を構成している頂点リスト</param>
        /// <param name="xNV">-x方向を向く面を構成している頂点リスト</param>
        /// <param name="yNV">-y方向を向く面を構成している頂点リスト</param>
        /// <param name="zNV">-z方向を向く面を構成している頂点リスト</param>
        /// <param name="__V">それ以外の面を構成している頂点リスト</param>
        static void ClassifyVertices(Mesh.MeshData meshData,
            List<CustomVertexLayout> xPV,
            List<CustomVertexLayout> yPV,
            List<CustomVertexLayout> zPV,
            List<CustomVertexLayout> xNV,
            List<CustomVertexLayout> yNV,
            List<CustomVertexLayout> zNV,
            List<CustomVertexLayout> __V)
        {
            var indexData = meshData.GetIndexData<int>();
            var vertexData = meshData.GetVertexData<CustomVertexLayout>(stream: 0);
            for (int i = 0; i < indexData.Length; i += 3)
            {
                // Triangles verts
                var vertex0 = vertexData[indexData[i + 0]];
                var vertex1 = vertexData[indexData[i + 1]];
                var vertex2 = vertexData[indexData[i + 2]];

                half halfLength = (half)0.5;
                var xP = vertex0.pos.x == halfLength && vertex1.pos.x == halfLength && vertex2.pos.x == halfLength;
                var yP = vertex0.pos.y == halfLength && vertex1.pos.y == halfLength && vertex2.pos.y == halfLength;
                var zP = vertex0.pos.z == halfLength && vertex1.pos.z == halfLength && vertex2.pos.z == halfLength;

                half nHalfLength = (half)(-0.5);
                var xN = vertex0.pos.x == nHalfLength && vertex1.pos.x == nHalfLength && vertex2.pos.x == nHalfLength;
                var yN = vertex0.pos.y == nHalfLength && vertex1.pos.y == nHalfLength && vertex2.pos.y == nHalfLength;
                var zN = vertex0.pos.z == nHalfLength && vertex1.pos.z == nHalfLength && vertex2.pos.z == nHalfLength;

                if (xP)
                {
                    xPV.Add(vertex0); xPV.Add(vertex1); xPV.Add(vertex2);
                }
                else if (yP)
                {
                    yPV.Add(vertex0); yPV.Add(vertex1); yPV.Add(vertex2);
                }
                else if (zP)
                {
                    zPV.Add(vertex0); zPV.Add(vertex1); zPV.Add(vertex2);
                }
                else if (xN)
                {
                    xNV.Add(vertex0); xNV.Add(vertex1); xNV.Add(vertex2);
                }
                else if (yN)
                {
                    yNV.Add(vertex0); yNV.Add(vertex1); yNV.Add(vertex2);
                }
                else if (zN)
                {
                    zNV.Add(vertex0); zNV.Add(vertex1); zNV.Add(vertex2);
                }
                else
                {
                    __V.Add(vertex0); __V.Add(vertex1); __V.Add(vertex2);
                }
            }
        }

        /// <summary>
        /// 分類された7つの頂点リストを一つのfileDataとしてフォーマット化
        /// </summary>
        /// <param name="xPV">+x方向を向く面を構成している頂点リスト</param>
        /// <param name="yPV">+y方向を向く面を構成している頂点リスト</param>
        /// <param name="zPV">+z方向を向く面を構成している頂点リスト</param>
        /// <param name="xNV">-x方向を向く面を構成している頂点リスト</param>
        /// <param name="yNV">-y方向を向く面を構成している頂点リスト</param>
        /// <param name="zNV">-z方向を向く面を構成している頂点リスト</param>
        /// <param name="__V">それ以外の面を構成している頂点リスト</param>
        /// <param name="fileData">フォーマット先ファイルデータ（関数呼び出し側でDisposeする必要あり）</param>
        static unsafe void FormatFileData(
            List<CustomVertexLayout> xPV,
            List<CustomVertexLayout> yPV, 
            List<CustomVertexLayout> zPV,
            List<CustomVertexLayout> xNV,
            List<CustomVertexLayout> yNV, 
            List<CustomVertexLayout> zNV, 
            List<CustomVertexLayout> __V, 
            out NativeArray<byte> fileData)
        {
            NativeArray<int> vertexCounts;
            byte* dataPtr;
            // caw + version
            var magicCode = new NativeArray<byte>(4, Allocator.Persistent);
            magicCode[0] = (byte)'c';
            magicCode[1] = (byte)'a';
            magicCode[2] = (byte)'w';
            magicCode[3] = 1;
            // vertex counts
            vertexCounts = new NativeArray<int>(7, Allocator.Persistent);
            vertexCounts[0] = xPV.Count;
            vertexCounts[1] = yPV.Count;
            vertexCounts[2] = zPV.Count;
            vertexCounts[3] = xNV.Count;
            vertexCounts[4] = yNV.Count;
            vertexCounts[5] = zNV.Count;
            vertexCounts[6] = __V.Count;
            var fileSize = magicCode.Length + vertexCounts.Length * sizeof(int) + vertexCounts.Sum() * sizeof(CustomVertexLayout);
            fileData = new NativeArray<byte>(fileSize, Allocator.Persistent);
            dataPtr = (byte*)fileData.GetUnsafePtr();
            var magicCodePtr = (byte*)magicCode.GetUnsafeReadOnlyPtr();
            var writeOffset = 0;
            for (int i = 0; i < magicCode.Length; i++)
            {
                dataPtr[i + writeOffset] = magicCodePtr[i];
            }
            writeOffset += magicCode.Length;
            magicCode.Dispose();
            var offsetCountsPtr = (byte*)vertexCounts.GetUnsafeReadOnlyPtr();
            for (int i = 0; i < vertexCounts.Length * sizeof(int); i++)
            {
                dataPtr[i + writeOffset] = offsetCountsPtr[i];
            }
            writeOffset += vertexCounts.Length * sizeof(int);
            vertexCounts.Dispose();

            var nativeXPV = new NativeArray<CustomVertexLayout>(xPV.ToArray(), Allocator.Persistent);
            var nativeXPVSize = nativeXPV.Length * sizeof(CustomVertexLayout);
            var xPVPtr = (byte*)nativeXPV.GetUnsafePtr();
            for (int i = 0; i < nativeXPVSize; i++)
            {
                dataPtr[i + writeOffset] = xPVPtr[i];
            }
            writeOffset += nativeXPVSize;
            nativeXPV.Dispose();

            var nativeYPV = new NativeArray<CustomVertexLayout>(yPV.ToArray(), Allocator.Persistent);
            var nativeYPVSize = nativeYPV.Length * sizeof(CustomVertexLayout);
            var yPVPtr = (byte*)nativeYPV.GetUnsafePtr();
            for (int i = 0; i < nativeYPVSize; i++)
            {
                dataPtr[i + writeOffset] = yPVPtr[i];
            }
            writeOffset += nativeYPVSize;
            nativeYPV.Dispose();

            var nativeZPV = new NativeArray<CustomVertexLayout>(zPV.ToArray(), Allocator.Persistent);
            var nativeZPVSize = nativeZPV.Length * sizeof(CustomVertexLayout);
            var zPVPtr = (byte*)nativeZPV.GetUnsafePtr();
            for (int i = 0; i < nativeZPVSize; i++)
            {
                dataPtr[i + writeOffset] = zPVPtr[i];
            }
            writeOffset += nativeZPVSize;
            nativeZPV.Dispose();

            var nativeXNV = new NativeArray<CustomVertexLayout>(xNV.ToArray(), Allocator.Persistent);
            var nativeXNVSize = nativeXNV.Length * sizeof(CustomVertexLayout);
            var xNVPtr = (byte*)nativeXNV.GetUnsafePtr();
            for (int i = 0; i < nativeXNVSize; i++)
            {
                dataPtr[i + writeOffset] = xNVPtr[i];
            }
            writeOffset += nativeXNVSize;
            nativeXNV.Dispose();

            var nativeYNV = new NativeArray<CustomVertexLayout>(yNV.ToArray(), Allocator.Persistent);
            var nativeYNVSize = nativeYNV.Length * sizeof(CustomVertexLayout);
            var yNVPtr = (byte*)nativeYNV.GetUnsafePtr();
            for (int i = 0; i < nativeYNVSize; i++)
            {
                dataPtr[i + writeOffset] = yNVPtr[i];
            }
            writeOffset += nativeYNVSize;
            nativeYNV.Dispose();

            var nativeZNV = new NativeArray<CustomVertexLayout>(zNV.ToArray(), Allocator.Persistent);
            var nativeZNVSize = nativeZNV.Length * sizeof(CustomVertexLayout);
            var zNVPtr = (byte*)nativeZNV.GetUnsafePtr();
            for (int i = 0; i < nativeZNVSize; i++)
            {
                dataPtr[i + writeOffset] = zNVPtr[i];
            }
            writeOffset += nativeZNVSize;
            nativeZNV.Dispose();

            var native__V = new NativeArray<CustomVertexLayout>(__V.ToArray(), Allocator.Persistent);
            var native__VSize = native__V.Length * sizeof(CustomVertexLayout);
            var __VPtr = (byte*)native__V.GetUnsafePtr();
            for (int i = 0; i < native__VSize; i++)
            {
                dataPtr[i + writeOffset] = __VPtr[i];
            }
            writeOffset += native__VSize;
            native__V.Dispose();
        }

        /// <summary>
        /// byte 配列をファイル出力
        /// </summary>
        /// <param name="byteArray">byte配列</param>
        /// <param name="filePath">ファイルパス</param>
        static unsafe void SaveBytesToFile(NativeArray<byte> byteArray, string filePath)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                writer.Write(byteArray);
            }
            Debug.Log($"saved to {filePath}");
        }
    }
}