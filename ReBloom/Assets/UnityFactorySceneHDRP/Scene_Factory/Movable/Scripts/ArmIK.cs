using System.Reflection;
using UnityEngine;





namespace UnityFactorySceneHDRP
{
	public class ArmIK : MonoBehaviour
	{
		[SerializeField] private Transform _stand;
		[SerializeField] private Transform _arm1;
		[SerializeField] private Transform _arm2;
		[SerializeField] private Transform _arm3;

		[Space(10)]
		[SerializeField] private Transform _arm1Base;
		[SerializeField] private Transform _target;


		private float _upperArmLength;
		private float _foreArmLength;



		private void Awake()
		{
			_upperArmLength = Vector3.Distance(_arm1.position, _arm2.position);
			_foreArmLength = Vector3.Distance(_arm2.position, _arm3.position);
		}



		private void Update()
		{
			float arm1Angle = Mathf.Atan2((_target.position - _stand.position).x, (_target.position - _stand.position).z) * Mathf.Rad2Deg;
			_stand.rotation = Quaternion.Euler(0, arm1Angle - 90, 0);

			#if UNITY_EDITOR
			_upperArmLength = Vector3.Distance(_arm1.position, _arm2.position);
			_foreArmLength = Vector3.Distance(_arm2.position, _arm3.position);
			#endif

			Vector2 targetLocalPos = _arm1Base.InverseTransformPoint(_target.position);
			float targetDistance = targetLocalPos.magnitude;

			if(targetDistance < _upperArmLength + _foreArmLength)
			{
				float angleA = Mathf.Asin(targetLocalPos.y / targetDistance) * Mathf.Rad2Deg;
				float angleB = Mathf.Acos((_upperArmLength * _upperArmLength + targetDistance * targetDistance - _foreArmLength * _foreArmLength) / (2 * _upperArmLength * targetDistance)) * Mathf.Rad2Deg;
				float angleC = Mathf.Acos((_upperArmLength * _upperArmLength + _foreArmLength * _foreArmLength - targetDistance * targetDistance) / (2 * _upperArmLength * _foreArmLength)) * Mathf.Rad2Deg;

				_arm1.localRotation = Quaternion.Euler(0, 0, -(90 - (angleA + angleB)));
				_arm2.localRotation = Quaternion.Euler(0, 0, -(180 - angleC));
			}
			else
			{
				float angleA = Mathf.Asin(targetLocalPos.y / targetDistance) * Mathf.Rad2Deg;

				_arm1.localRotation = Quaternion.Euler(0, 0, -(90 - angleA));
				_arm2.localRotation = Quaternion.identity;
			}
		}
	}
}