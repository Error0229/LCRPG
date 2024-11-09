using System.Collections;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public GameObject HealthValue;
    public GameObject HealthBarContainer;
    private int _health;
    private int _maxHealth;
    private float _originHealthScale;

    private void Awake()
    {
    }

    public void Reset()
    {
        print("reset health bar");
        _health = _maxHealth;
        HealthValue.transform.localScale = new Vector3(_originHealthScale, HealthValue.transform.localScale.y,
            HealthValue.transform.localScale.z);
    }

    public void SetMaxHealth(int health)
    {
        _maxHealth = health;
        _health = health;
        _originHealthScale = HealthValue.transform.localScale.x;
    }

    public void Disable()
    {
        HealthValue.SetActive(false);
        HealthBarContainer.SetActive(false);
    }

    public void UpdateHealth(int health)
    {
        StartCoroutine(HealthBarAnimation(health));
    }

    public IEnumerator HealthBarAnimation(float health)
    {
        var oriScale = HealthValue.transform.localScale;
        var targetScale = health / _maxHealth * _originHealthScale;
        var elapsedTime = 0f;
        var duration = 0.3f;
        while (elapsedTime < duration)
        {
            HealthValue.transform.localScale =
                new Vector3(Mathf.Lerp(oriScale.x, targetScale, elapsedTime / duration), oriScale.y, oriScale.z);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    public void Hurt(int damage)
    {
        UpdateHealth(_health - damage);
    }
}