using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SC_DistortionEffect : MonoBehaviour
{
    public SC_PhysicObject car;
    public Material m_mat;

    public float blur_max = 0.6f;
    public float chroma_max = 0.02f;
    public float vignette_max = 0.4f;
    public float speed_threshold = 0.2f; // only kicks in above 60% of top speed


    void Update()
    {
        float speed_t = Mathf.Clamp01(car.current_velocity / (car.speed * 1.5f));
        float t = Mathf.Clamp01((speed_t - speed_threshold) / (1 - speed_threshold));

        m_mat.SetFloat("_BlurAmount", t * blur_max);
        m_mat.SetFloat("_ChromaAmount", t * chroma_max);
        m_mat.SetFloat("_VignetteAmount", t * vignette_max);
    }
}