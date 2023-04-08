using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
//TODO test diffs between delcaring in-job vs class-access
public class RotationJob : MonoBehaviour{

    public Mesh mesh;
    public Material mat;
    

    public static (int x, int y) size = (100, 100);
    public static int bufferSize = 784;
    public static Vector3 scale = Vector3.one;

    public static float totalTime;
    public static NativeArray<float3> positions;
    public static NativeArray<Matrix4x4> matrices;

    JobHandle handle;
    RenderParams rp;
    UpdatePositionsJob job;

    [Unity.Burst.BurstCompile]
    public struct UpdatePositionsJob : IJobParallelFor{

        public NativeArray<float3> positions;
        public NativeArray<Matrix4x4> matrices;
        public float totalTime;

        public void Execute(int index){
            float3 temp = positions[index];
            temp.y = noise.cnoise(temp.xz + totalTime);
            positions[index] = temp;
            matrices[index] = Matrix4x4.TRS(positions[index], quaternion.identity, math.float3(1f, 1f, 1f));
        }
    }

    void Start(){
        positions = new NativeArray<float3>(size.x * size.y, Allocator.Persistent);
        matrices = new NativeArray<Matrix4x4>(size.x * size.y, Allocator.Persistent);

        for(int i = 0; i <= size.x-1; i++){
            for(int j = 0; j <= size.y-1; j++){
                positions[j + i * size.y] = new float3(i, 0, j);
            }
        }
        job = new UpdatePositionsJob();
        job.positions = positions; job.matrices = matrices;
        rp = new RenderParams(mat);
    }

    void Update(){

        job.totalTime = totalTime;
        totalTime = Time.realtimeSinceStartup;
        handle = job.Schedule(size.x * size.y, 1);
        handle.Complete();

        int i;
        for(i = 0; i <= (size.x * size.y) - bufferSize; i += 784){
            Graphics.RenderMeshInstanced(rp, mesh, 0, matrices.GetSubArray(i, bufferSize), size.x * size.y, 0);
        }
        Graphics.RenderMeshInstanced(rp, mesh, 0, matrices.GetSubArray(i, size.x*size.y-i), size.x * size.y, 0);
        
    }

    void LateUpdate(){
    }

    void OnDestroy(){
        positions.Dispose();
        matrices.Dispose();
    }

}