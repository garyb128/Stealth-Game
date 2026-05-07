using UnityEngine;

public static class Ballistics
{
    public static bool FireBullet(
        Vector3 origin,
        Vector3 direction,
        float muzzleVelocity,
        float gravity,
        float maxSimulationTime,
        out RaycastHit hit)
    {
        hit = default;

        Vector3 currentPosition = origin;

        // Initial bullet velocity
        Vector3 velocity = direction.normalized * muzzleVelocity;

        // Smaller = more accurate
        float stepTime = 0.02f;

        float elapsedTime = 0f;

        while (elapsedTime < maxSimulationTime)
        {
            Vector3 previousPosition = currentPosition;

            // Move bullet
            currentPosition += velocity * stepTime;

            // Apply gravity
            velocity += Vector3.down * gravity * stepTime;

            // Check if bullet hit something
            if (Physics.Linecast(previousPosition, currentPosition, out hit))
            {
                return true;
            }

            // Debug trajectory
            Debug.DrawLine(previousPosition, currentPosition, Color.red, 0.5f);

            elapsedTime += stepTime;
        }

        return false;
    }
}