using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string transitionTo;

    [SerializeField] private Transform startPoint;

    [SerializeField] private Vector2 exitDirection;

    [SerializeField] private float exitTime;

    private void Start()
    {
        if(transitionTo == GameManager.Instance.transitionedFromScene)
        {
            PlayerController.Instance.transform.position = startPoint.position; //Tells where the player is to be put upon transitioning from a scene.
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.CompareTag("Player"))
        {
            GameManager.Instance.transitionedFromScene = SceneManager.GetActiveScene().name;

            SceneManager.LoadScene(transitionTo);
        }
    }
}