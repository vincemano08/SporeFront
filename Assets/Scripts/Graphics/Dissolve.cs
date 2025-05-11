using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dissolve : MonoBehaviour {
    [SerializeField] private bool isHidden = false;
    [SerializeField] private float animationDuration = 1.0f;

    private float _currentDissolve;
    private Coroutine _dissolveCoroutine;
    private List<Material> _dissolveMaterials;

    // The name of the property in the Shader Graph
    private const string DissolvePropertyName = "_Dissolve";

    void Awake() {
        _dissolveMaterials = new List<Material>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0) return;

        foreach (var renderer in renderers) {
            Material[] materials = renderer.materials;
            if (materials != null && materials.Length > 0) {
                foreach (var mat in materials) {
                    if (mat.HasProperty(DissolvePropertyName)) {
                        _dissolveMaterials.Add(mat);
                    }
                }
            }
        }

        // Initialize the current dissolve amount based on the initial isHidden state.
        _currentDissolve = isHidden ? 1.0f : 0.0f;

        // Set the initial state on all material instances.
        foreach (var mat in _dissolveMaterials) {
            mat.SetFloat(DissolvePropertyName, _currentDissolve);
        }
        
    }

    public void ToggleDissolve() {
        isHidden = !isHidden;
    }

    public void SetDissolve(bool hidden) {
        isHidden = hidden;
    }

    void Update() {
        if (_dissolveMaterials == null || _dissolveMaterials.Count == 0) {
            Debug.LogError("Dissolve Material is not assigned or found!");
            return;
        }

        float currentShaderAmount = _dissolveMaterials[0].GetFloat(DissolvePropertyName);
        float targetAmount = isHidden ? 1.0f : 0.0f;

        if (Mathf.Approximately(currentShaderAmount, targetAmount)) {
            // Already at the target state, stop any running animation
            if (_dissolveCoroutine != null) {
                StopCoroutine(_dissolveCoroutine);
                _dissolveCoroutine = null;
            }
        } else {
            // Target state is different, start the animation
            StartDissolveAnimation(targetAmount);
        }
    }


    // Starts the dissolve animation coroutine towards a target amount
    private void StartDissolveAnimation(float targetAmount) {
        // Stop any existing animation first
        if (_dissolveCoroutine != null) {
            StopCoroutine(_dissolveCoroutine);
        }

        _dissolveCoroutine = StartCoroutine(AnimateDissolve(_currentDissolve, targetAmount, animationDuration));
    }


    private IEnumerator AnimateDissolve(float startAmount, float endAmount, float duration) {
        float startTime = Time.time;
        float endTime = startTime + duration;

        // Ensure the material is valid before trying to set properties
        if (_dissolveMaterials == null) {
            Debug.LogError("Dissolve Material is not assigned or found!");
            _dissolveCoroutine = null; 
            yield break;
        }

        while (Time.time < endTime) {
            float t = (Time.time - startTime) / duration;

            // Interpolate the dissolve amount
            _currentDissolve = Mathf.Lerp(startAmount, endAmount, t);
            foreach (var mat in _dissolveMaterials) {
                mat.SetFloat(DissolvePropertyName, _currentDissolve);
            }

            // Wait for the next frame
            yield return null;
        }

        // Ensure the value is exactly the target amount at the end
        _currentDissolve = endAmount;
        foreach (var mat in _dissolveMaterials) {
            mat.SetFloat(DissolvePropertyName, _currentDissolve);
        }

        _dissolveCoroutine = null;
    }
}