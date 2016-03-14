using UnityEngine;
using System.Collections;

[RequireComponent (typeof (BoxCollider2D))]
public class Controller : MonoBehaviour {
	
	//assign the layer which to collide with
	public LayerMask collisionMask;

	//add a thin skin to make the hit box slightly smaller than our object
	const float skinWidth = .015f;
	//number of raycasts for left right up down
	public int horRayCount = 4;
	public int verRayCount = 4;

	//the max angles for slope interactions, don't want to go up vertical walls
	float maxClimbAngle = 80;
	float maxDescendAngle = 80;

	//space the rays, calculated from object width/length over ray count
	float horizontalRaySpacing;
	float verticalRaySpacing;

	//boxcollider component of game object
	BoxCollider2D collider;
	//identifier for struct for top left/right, bottom left/right for casting geometry
	RaycastOrigins raycastOrigins;
	//identifier for struct for collisions
	public CollisionInfo collisions;

	void Start() {
		//get game objects boxcollider component
		collider = GetComponent<BoxCollider2D> ();
		//self explanatory
		CalculateRaySpacing ();
	}

	public void Move(Vector3 velocity) {
		//update ray casting based on position
		UpdateRaycastOrigins ();
		//reset collision info
		collisions.Reset ();
		//hold ref to old velocity
		collisions.velocityOld = velocity;

		if (velocity.y < 0) {
			//if falling/going down, check if on a slope
			DescendSlope(ref velocity);
		}
		if (velocity.x != 0) {
			//if moving left/right, check for walls
			HorizontalCollisions (ref velocity);
		}
		if (velocity.y != 0) {
			//if jumping/falling and not on a slope, check for ceiling/floors
			VerticalCollisions (ref velocity);
		}

		//translate game object
		transform.Translate (velocity.x, velocity.y, 0);
	}

	void HorizontalCollisions(ref Vector3 velocity) {
		//get the sign on our horizontal velocity, -1 for left, +1 for right
		float directionX = Mathf.Sign (velocity.x);
		//raycast based on our velocity, compensate for skin width
		float rayLength = Mathf.Abs (velocity.x) + skinWidth;

		for (int i = 0; i < horRayCount; i ++) {
			Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength,Color.red);

			if (hit) {

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

				if (i == 0 && slopeAngle <= maxClimbAngle) {
					if (collisions.goingDown) {
						collisions.goingDown = false;
						velocity = collisions.velocityOld;
					}
					float distanceToSlopeStart = 0;
					if (slopeAngle != collisions.slopeAngleOld) {
						distanceToSlopeStart = hit.distance-skinWidth;
						velocity.x -= distanceToSlopeStart * directionX;
					}
					ClimbSlope(ref velocity, slopeAngle);
					velocity.x += distanceToSlopeStart * directionX;
				}

				if (!collisions.goingUp || slopeAngle > maxClimbAngle) {
					velocity.x = (hit.distance - skinWidth) * directionX;
					rayLength = hit.distance;

					if (collisions.goingUp) {
						velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
					}

					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}

	bool Titanic(bool iceberg) {
		bool floats = false;
		return floats;
	}

	void VerticalCollisions(ref Vector3 velocity) {
		float directionY = Mathf.Sign (velocity.y);
		float rayLength = Mathf.Abs (velocity.y) + skinWidth;

		for (int i = 0; i < verRayCount; i ++) {
			Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength,Color.red);

			if (hit) {
				velocity.y = (hit.distance - skinWidth) * directionY;
				rayLength = hit.distance;

				if (collisions.goingUp) {
					velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;
			}
		}

		if (collisions.goingUp) {
			float directionX = Mathf.Sign(velocity.x);
			rayLength = Mathf.Abs(velocity.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up * velocity.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right * directionX,rayLength,collisionMask);

			if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
				if (slopeAngle != collisions.slopeAngle) {
					velocity.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
				}
			}
		}
	}

	void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
		float moveDistance = Mathf.Abs (velocity.x);
		float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (velocity.y <= climbVelocityY) {
			velocity.y = climbVelocityY;
			velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
			collisions.below = true;
			collisions.goingUp = true;
			collisions.slopeAngle = slopeAngle;
		}
	}

	void DescendSlope(ref Vector3 velocity) {
		float directionX = Mathf.Sign (velocity.x);
		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

		if (hit) {
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
				if (Mathf.Sign(hit.normal.x) == directionX) {
					if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) {
						float moveDistance = Mathf.Abs(velocity.x);
						float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
						velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
						velocity.y -= descendVelocityY;

						collisions.slopeAngle = slopeAngle;
						collisions.goingDown = true;
						collisions.below = true;
					}
				}
			}
		}
	}

	void UpdateRaycastOrigins() {
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth * -2);

		raycastOrigins.bottomLeft = new Vector2 (bounds.min.x, bounds.min.y);
		raycastOrigins.bottomRight = new Vector2 (bounds.max.x, bounds.min.y);
		raycastOrigins.topLeft = new Vector2 (bounds.min.x, bounds.max.y);
		raycastOrigins.topRight = new Vector2 (bounds.max.x, bounds.max.y);
	}

	void CalculateRaySpacing() {
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth * -2);

		horRayCount = Mathf.Clamp (horRayCount, 2, int.MaxValue);
		verRayCount = Mathf.Clamp (verRayCount, 2, int.MaxValue);

		horizontalRaySpacing = bounds.size.y / (horRayCount - 1);
		verticalRaySpacing = bounds.size.x / (verRayCount - 1);
	}

	struct RaycastOrigins {
		public Vector2 topLeft;
		public Vector2 topRight;
		public Vector2 bottomLeft;
		public Vector2 bottomRight;
	}

	public struct CollisionInfo {
		public bool above;
		public bool below;
		public bool left;
		public bool right;

		public bool goingUp;
		public bool goingDown;
		public float slopeAngle;
		public float slopeAngleOld;
		public Vector3 velocityOld;

		public void Reset() {
			above = false;
			below = false;
			left = false;
			right = false;
			goingUp = false;
			goingDown = false;

			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}
}
