using System.Collections;
using UnityEngine;


/// <summary>
/// Can be used to randomly shake any SharedVector3 over time. 
/// Can be used for camera shake or to shake any other arbitrary objects.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable OBJ/Vector Shaker", fileName = "New Vector Shaker")]
public class VectorShaker : ScriptableObject
{
	// Inspector-Assigned
	[Tooltip("The vector we wish to shake")]
	[SerializeField] private SharedVector3 shakeVector = null;

	// Internals 
	protected IEnumerator coroutine = null;


	public void ShakeVector(float _duration, float _magnitude, float _damping = 1.0f)
	{
		if (_duration < 0.001f || _magnitude.Equals(0.0f))
			return;

		if (shakeVector != null && SO_CoroutineRunner.Instance != null)
		{
			if(coroutine != null)
			{
				SO_CoroutineRunner.Instance.StopCoroutine(coroutine);
			}

			coroutine = Shake(_duration, _magnitude, _damping);

			SO_CoroutineRunner.Instance.StartCoroutine(coroutine);
		}
	}

	protected IEnumerator Shake(float _duration, float _magnitude, float _damping = 1.0f)
	{
		float time = 0.0f;

		while (time <= _duration)
		{
			float x = Random.Range(-1.0f, 1.0f) * _magnitude;
			float y = Random.Range(-1.0f, 1.0f) * _magnitude;

			Vector3 unSmoothedVector = new Vector3(x, y, 0.0f);

			if(shakeVector != null)
			{
				shakeVector.Value = Vector3.Lerp(unSmoothedVector, Vector3.zero, (time / _duration) * _damping);
			}

			time += Time.deltaTime;

			yield return null;
		}

		shakeVector.Value = Vector3.zero;
	}
}