using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultyButton : MonoBehaviour
{
    public Difficulty diff;
    public ShootingRange shootingRange;

    public void SelectDifficulty(Difficulty _diff)
    {
        shootingRange.DifficultySelect(_diff);
    }
}
