using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [Header("Health")]
    public GameObject healthBar;
    [SerializeField] private List<Transform> healthBarComponents;
    public Color activeHealth;
    public Color inactiveHealth;
    public float health;
    public float maxHealth;

    private void Awake()
    {
        foreach (Transform component in healthBar.transform)
        {
            healthBarComponents.Add(component);
        }

        health = maxHealth;
    }

    private void Update()
    {
        if (health <= 0)
        {
            Death();
        }

        UpdateBar(healthBarComponents, activeHealth, inactiveHealth, health, maxHealth);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("death"))
        {
            Death();
        }
    }

    public void Death()
    {
        transform.position = new Vector2(-12.69f, 6.56f);
        Respawn();
    }

    public void Respawn()
    {
        health = maxHealth;
    }

    public void UpdateBar(List<Transform> list, Color active, Color inactive, float value, float maxValue)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Image image = list[i].GetComponent<Image>();
            
            if ((i / 2) < maxValue)
            {
                list[i].gameObject.SetActive(true);

                if (((i + 0.02f) / 2) > value)
                {
                    image.color = inactive;
                }
                else
                {
                    image.color = active;
                }
            }
            else
            {
                list[i].gameObject.SetActive(false);
            }
        }
    }
}
