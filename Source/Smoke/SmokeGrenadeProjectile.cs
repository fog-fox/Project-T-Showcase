using System.Collections;
using UnityEngine;

public class SmokeGrenadeProjectile : MonoBehaviour, IThrowableProjectile
{
    private ThrowableData throwableData;
    private Vector3 targetPosition;
    private float moveSpeed;
    private bool initialized = false;

    public void Initialize(ThrowableData data, Vector3 targetPos, float speed)
    {
        throwableData = data;
        targetPosition = targetPos;
        moveSpeed = speed;
        initialized = true;

        StartCoroutine(MoveAndDeploySmoke());
    }

    private IEnumerator MoveAndDeploySmoke()
    {
        if (!initialized || throwableData == null)
            yield break;

        while (Vector2.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;

        yield return new WaitForSeconds(throwableData.fuseTime);

        DeploySmoke();
    }

    private void DeploySmoke()
    {
        if (throwableData.smokeFieldPrefab == null)
        {
            Debug.LogError("SmokeGrenadeProjectile: smokeFieldPrefab이 비어 있습니다.");
            Destroy(gameObject);
            return;
        }

        GameObject smokeFieldObj = Instantiate(
            throwableData.smokeFieldPrefab,
            transform.position,
            Quaternion.identity
        );

        SmokeField smokeField = smokeFieldObj.GetComponent<SmokeField>();
        if (smokeField == null)
        {
            Debug.LogError("SmokeGrenadeProjectile: smokeFieldPrefab에 SmokeField 컴포넌트가 없습니다.");
            Destroy(smokeFieldObj);
            Destroy(gameObject);
            return;
        }

        smokeField.Initialize(throwableData);

        Debug.Log($"{throwableData.throwableName} 연막 전개");
        Destroy(gameObject);
    }
}