using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    [SerializeField, Range(10, 200)] int resolution = 10;
    [SerializeField] FunctionLibrary.FunctionName function;
    [SerializeField] TransitionMode transitionMode;
    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;
    [SerializeField] ComputeShader computeShader;

    private ComputeBuffer positionsBuffer;

    private static readonly int positionsId = Shader.PropertyToID("_Property");
    private static readonly int resolutionId = Shader.PropertyToID("_Resolution");
    private static readonly int timeId = Shader.PropertyToID("_Time");
    private static readonly int stepId = Shader.PropertyToID("_Step");

    public enum TransitionMode
    {
        Cycle,
        Random
    }

    private Transform[] points;
    private float timeElapsedInFunction;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetFloat(stepId, step);
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(timeId, Time.time);
        computeShader.SetBuffer(0, positionsId, positionsBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(0, groups, groups, 1);
    }
    
    void Update()
    {
        timeElapsedInFunction += Time.deltaTime;
        if (transitioning)
        {
            if (timeElapsedInFunction >= functionDuration)
            {
                timeElapsedInFunction -= functionDuration;
                transitioning = false;
            }
        }
        else if (timeElapsedInFunction >= functionDuration)
        {
            timeElapsedInFunction -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }
        UpdateFunctionOnGPU();
    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle
            ? FunctionLibrary.GetNextFunctionName(function)
            : FunctionLibrary.GetRandomFunctionOtherThan(function);
    }
}