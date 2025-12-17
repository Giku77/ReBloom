using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpawnEffect : MonoBehaviour
{
    [SerializeField] private List<Renderer> _renderers;
    [SerializeField] private List<Material> mtrlOrg;
    [SerializeField] private Material mtrlDissolve;
    [SerializeField] private float fadeTime = 2f;

    private Material _instanceMaterial;
    public void PlayEffect()
    {
        _instanceMaterial = new Material(mtrlDissolve);
        for(int i = 0; i < _renderers.Count; i++)
        {
            _renderers[i].sharedMaterial = _instanceMaterial;
        }
        DoFade(0f, 1f, fadeTime);

        //_instanceMaterial = new Material(mtrlPhase);
        //_renderer.material = _instanceMaterial;
        //DoFade(0f, 2f, fadeTime);
    }

    void DoFade(float start, float dest, float time)
    {
        _instanceMaterial.SetFloat("_SpiltValue", start);

        DOTween.To(
            () => _instanceMaterial.GetFloat("_SpiltValue"),
            x => _instanceMaterial.SetFloat("_SpiltValue", x),
            dest,
            time
        )
        .SetEase(Ease.OutCubic)
        .OnUpdate(() =>
        {
            if (_instanceMaterial.GetFloat("_SpiltValue") > 0.95f)
            {
                OnFadeComplete();
            }
        });
    }

    void OnFadeComplete()
    {
        for (int i = 0; i < _renderers.Count; i++)
        {
            _renderers[i].sharedMaterial = mtrlOrg[i];
        }

        // 인스턴스 머티리얼 정리
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
            _instanceMaterial = null;
        }
    }

    void OnDestroy()
    {
        // 오브젝트 파괴 시 머티리얼 정리
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
        }
    }
}