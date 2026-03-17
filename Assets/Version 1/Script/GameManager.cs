using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public bool isCorrect = false;
    void Start()
    {
        isCorrect=true;//= LoadCorrect();
    }

}
