// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

//#define NGUI_SUPPORT

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using WorldMapStrategyKit.PolygonClipping;
using Random = UnityEngine.Random;

namespace WorldMapStrategyKit
{
	[Serializable, ExecuteInEditMode]
	public partial class WMSK : MonoBehaviour
	{
		public const float MAP_PRECISION = 5000000f;
		public const string SURFACE_LAYER = "Surfaces";

		private const float TAP_THRESHOLD = 0.25f;
		private const string OVERLAY_BASE = "OverlayLayer";
		private const string SKW_BUMPMAP_ENABLED = "WMSK_BUMPMAP_ENABLED";
		private char[] SPLIT_SEP_SEMICOLON = { ';' };
		private char[] SPLIT_SEP_ASTERISK = { '*' };

		public static float mapWidth => instanceExists ? instance.transform.localScale.x : 200.0f;

		public static float mapHeight => instanceExists ? instance.transform.localScale.y : 100.0f;

		#region Internal variables

		// resources
		private Material coloredMat, coloredAlphaMat, texturizedMat;
		private Material outlineMatSimple, outlineMatTextured, cursorMatH, cursorMatV, imaginaryLinesMat;
		private Material markerMat, markerLineMat;
		private Material earthMat;
		private Material rectangleSelectionMat;

		// gameObjects
		private GameObject _surfacesLayer;

		private GameObject surfacesLayer
		{
			get
			{
				if (_surfacesLayer == null)
					CreateSurfacesLayer();
				return _surfacesLayer;
			}
		}

		private GameObject cursorLayerHLine, cursorLayerVLine, latitudeLayer, longitudeLayer;
		private GameObject markersLayer;

		// caché and gameObject lifetime control
		private Dictionary<int, GameObject> surfaces;
		private Dictionary<Color, Material> coloredMatCache;
		private Dictionary<Color, Material> markerMatCache;
		private Dictionary<double, Region> frontiersCacheHit;
		private List<Vector2> frontiersPoints;
		private DisposalManager disposalManager;
		private int lastRegionIndex;

		// FlyTo functionality
		private Quaternion flyToStartQuaternion, flyToEndQuaternion;
		private Vector3 flyToStartLocation, flyToEndLocation;
		private bool flyToActive;
		private float flyToStartTime, flyToDuration;
		private Vector3 flyToCallParamsPoint;
		private float flyToCallZoomDistance;

		// UI interaction variables
		private Vector3 mouseDragStart, dragDirection, mouseDragStartHitPos;

		private float dragDampingStart,
			dragSpeed,
			maxFrustumDistance,
			lastDistanceFromCamera,
			distanceFromCameraStartingFrame;

		private float wheelAccel, zoomDampingStart;
		private bool pinching, dragging, hasDragged, lastMouseMapHitPosGood;
		private float clickTime;
		private float lastCamOrtographicSize;
		private Vector3 lastMapPosition, lastCamPosition;
		private Vector3 lastMouseMapHitPos;
		private bool shouldCheckBoundaries;

		// raycasting reusable buffer
		private RaycastHit[] tempHits;

		private bool canInteract = true;

		// used in viewport mode changes
		private Vector3 lastKnownMapCoordinates;

		/// <summary>
		/// The last known zoom level. Updated when zooming in/out.
		/// </summary>
		public float lastKnownZoomLevel;

		// Overlay (Labels, tickers, ...)
		private GameObject overlayLayer;

		// Earth effects
		private RenderTexture earthBlurred;

		private int layerMask
		{
			get
			{
				if (Application.isPlaying && renderViewportIsEnabled)
					return 1 << renderViewport.layer;
				return 1 << gameObject.layer;
			}
		}

		private bool miniMapChecked;
		private bool _isMiniMap;

		/// <summary>
		/// Returns true if this is a minimap. Set internally by minimap component.
		/// </summary>
		private bool isMiniMap
		{
			get
			{
				if (!miniMapChecked)
				{
					_isMiniMap = GetComponent<WMSKMiniMap>() != null;
					miniMapChecked = true;
				}
				return _isMiniMap;
			}
		}

		private bool updateDoneThisFrame;

		public bool isDirty;
		// internal variable used to confirm changes - don't change its value

		private Material outlineMat
		{
			get
			{
				if (_outlineDetail == OUTLINE_DETAIL.Textured)
					return outlineMatTextured;
				return outlineMatSimple;
			}
		}

		#endregion

		#region Game loop events

		private void OnEnable()
		{
			if (_countries == null)
				Init();
		}

		private void OnDisable()
		{
			if (isMiniMap)
				return;
			ToggleWrapCamera(false);
			if (_currentCamera != null && _currentCamera.name.Equals(MAPPER_CAM))
				_currentCamera.enabled = false;
			ToggleUIPanel(true);
		}

		private void OnDestroy()
		{
			if (_surfacesLayer != null)
				DestroyImmediate(_surfacesLayer);
			if (coloredMatCache != null)
				foreach (var mat in coloredMatCache.Values)
					if (mat != null)
						DestroyImmediate(mat);
			if (markerMatCache != null)
				foreach (var mat in markerMatCache.Values)
					if (mat != null)
						DestroyImmediate(mat);
			overlayLayer = null;
			DestroyMapperCam();
			DestroyTiles();
			if (disposalManager != null)
				disposalManager.DisposeAll();
		}

		private void Reset()
		{
			Redraw();
		}

		private void Update()
		{
			if (currentCamera == null || !Application.isPlaying)
			{
				// For some reason, when saving the scene, the renderview port loses the attached rendertexture.
				// No event is fired, except for Update(), so we need to refresh the attached rendertexture of the render viewport here.
				SetupViewport();
				CheckRectConstraints();
				return;
			}

			if (updateDoneThisFrame)
				return;
			updateDoneThisFrame = true;

			// Updates mapperCam to reflect current main camera position and rotation (if main camera has moved)
			if (renderViewPortIsTerrain)
				SyncMapperCamWithMainCamera();

			// Check if navigateTo... has been called and in this case scrolls the map until the country is centered
			if (flyToActive)
				MoveCameraToDestination();

			// Check Viewport scale
			CheckViewportScaleAndCurvature();

			// Check whether the points is on an UI element, then avoid user interaction
			if (respectOtherUI && !hasDragged)
			{
				if (EventSystem.current != null)
				{
					if (Application.isMobilePlatform &&
					    Input.touchCount > 0 &&
					    EventSystem.current.IsPointerOverGameObject(
						    Input.GetTouch(0).fingerId))
						canInteract = false;
					else if (EventSystem.current.IsPointerOverGameObject(-1))
						canInteract = false;
				}

#if NGUI_SUPPORT
																if (UICamera.hoveredObject != null && !UICamera.hoveredObject.name.Equals("UI Root")) {
																				canInteract = false;
																}
#endif

				if (!canInteract)
					mouseIsOverUIElement = true;
			}

			var prevCamPos = _currentCamera.transform.position;
			if (canInteract)
			{
				CheckCursorVisibility();
				mouseIsOverUIElement = false;
				PerformUserInteraction();
			}
			else if (!Input.GetMouseButton(0))
				canInteract = true;

			if (isMiniMap)
				return;

			// Check boundaries
			if (transform.position != lastMapPosition ||
			    _currentCamera.transform.position != lastCamPosition ||
			    _currentCamera.orthographicSize != lastCamOrtographicSize)
				shouldCheckBoundaries = true;

			if (shouldCheckBoundaries)
			{
				shouldCheckBoundaries = false;

				shouldCheckTiles = true;
				resortTiles = true;

				// Last distance
				if (_currentCamera.orthographic)
				{
					_currentCamera.orthographicSize = Mathf.Clamp(_currentCamera.orthographicSize, 1,
						maxFrustumDistance);
					// updates frontiers LOD
					frontiersMat.shader.maximumLOD = _currentCamera.orthographicSize < 2.2 ? 100 :
						_currentCamera.orthographicSize < 8 ? 200 : 300;
					provincesMat.shader.maximumLOD = _currentCamera.orthographicSize < 8 ? 200 : 300;
				}
				else
				{
					if (!_enableFreeCamera &&
					    (_allowUserZoom || flyToActive) &&
					    (_zoomMinDistance > 0 || _zoomMaxDistance > 0))
					{
						var minDistance = Mathf.Max(_currentCamera.nearClipPlane + 0.0001f,
							_zoomMinDistance * transform.localScale.y);
						var plane = new Plane(transform.forward, transform.position);
						lastDistanceFromCamera =
							Mathf.Abs(plane.GetDistanceToPoint(_currentCamera.transform.position));
						if (lastDistanceFromCamera < minDistance)
						{
							_currentCamera.transform.position = ClampDistanceToMap(prevCamPos,
								_currentCamera.transform.position, lastDistanceFromCamera, minDistance);
							lastDistanceFromCamera = minDistance;
							wheelAccel = 0;
						}
						else
						{
							var maxDistance = Mathf.Min(maxFrustumDistance,
								_zoomMaxDistance * transform.localScale.y);
							if (lastDistanceFromCamera > maxDistance)
							{
								// Get intersection point from camera with plane
								_currentCamera.transform.position = ClampDistanceToMap(prevCamPos,
									_currentCamera.transform.position, lastDistanceFromCamera,
									maxDistance);
								lastDistanceFromCamera = maxDistance;
								wheelAccel = 0;
							}
						}
					}
					// updates frontiers LOD
					UpdateShadersLOD();
				}

				// Constraint to limits if user interaction is enabled
				if (!_enableFreeCamera && (_allowUserDrag || _allowUserZoom || _allowUserKeys))
					CheckRectConstraints();
				lastMapPosition = transform.position;
				lastCamPosition = _currentCamera.transform.position;
				lastCamOrtographicSize = _currentCamera.orthographicSize;
				lastMouseMapHitPosGood = false; // forces check again CheckMousePos()
				lastRenderViewportGood = false; // forces calculation of the viewport rect

				// Map has moved: apply changes
				if (distanceFromCameraStartingFrame != lastDistanceFromCamera)
				{
					distanceFromCameraStartingFrame = lastDistanceFromCamera;

					// Update distance param in ScenicPlus material
					if (_earthStyle.isScenicPlus())
						UpdateScenicPlusDistance();
					// Fades country labels
					if (_showCountryNames)
						FadeCountryLabels();
					// Fades country labels
					if (_showProvinceNames)
						FadeProvinceLabels();

					// Check maximum screen area size for highlighted country
					if (_highlightMaxScreenAreaSize < 1f)
					{
						if (_countryRegionHighlighted != null &&
						    countryRegionHighlightedObj != null &&
						    countryRegionHighlightedObj.activeSelf)
							if (!CheckScreenAreaSizeOfRegion(_countryRegionHighlighted))
								countryRegionHighlightedObj.SetActive(false);
						if (_provinceRegionHighlighted != null &&
						    provinceRegionHighlightedObj != null &&
						    provinceRegionHighlightedObj.activeSelf)
							if (!CheckScreenAreaSizeOfRegion(_provinceRegionHighlighted))
								provinceRegionHighlightedObj.SetActive(false);
					}

					if (_showTiles)
					{
						if (frontiersLayer != null)
						{
							if (_currentZoomLevel > _tileLinesMaxZoomLevel && frontiersLayer.activeSelf)
								frontiersLayer.SetActive(false);
							else if (_showFrontiers &&
							         _currentZoomLevel <= _tileLinesMaxZoomLevel &&
							         !frontiersLayer.activeSelf)
								frontiersLayer.SetActive(true);
						}
						if (provincesObj != null)
						{
							if (_currentZoomLevel > _tileLinesMaxZoomLevel && provincesObj.activeSelf)
								provincesObj.SetActive(false);
							else if (_showProvinces &&
							         _currentZoomLevel <= _tileLinesMaxZoomLevel &&
							         !provincesObj.activeSelf)
								provincesObj.SetActive(true);
						}
						if (lastCountryOutlineRef != null)
						{
							if (_currentZoomLevel > _tileLinesMaxZoomLevel &&
							    lastCountryOutlineRef.activeSelf)
								lastCountryOutlineRef.SetActive(false);
							else if (_currentZoomLevel <= _tileLinesMaxZoomLevel &&
							         !lastCountryOutlineRef.activeSelf)
								lastCountryOutlineRef.SetActive(true);
						}
					}
				}

				// Update everything related to viewport
				lastRenderViewportGood = false;
				if (renderViewportIsEnabled)
					UpdateViewport();

				// Update grid
				if (_showGrid)
					CheckGridRect();
			}
			else if (!renderViewPortIsTerrain)
			{
				// Map has not moved
				if (--viewportColliderNeedsUpdate == 1)
				{
					Mesh ms;
					if (viewportIndices.Length >= 25000)
						ms = flexQuad;
					else
						ms = _renderViewport.GetComponent<MeshFilter>().sharedMesh;
					if (ms.vertexCount >= 4)
					{
						var mc = _renderViewport.GetComponent<MeshCollider>();
						if (mc != null)
						{
							if (mc.convex)
								mc.convex = false;
							mc.sharedMesh = null;
							mc.sharedMesh = ms;
						}
					}
					viewportColliderNeedsUpdate = 0;
				}

				// Check if viewport rotation has changed or has moved
				if (renderViewportIsEnabled)
				{
					if (_renderViewport.transform.localRotation.eulerAngles !=
					    lastRenderViewportRotation ||
					    _renderViewport.transform.position != lastRenderViewportPosition)
					{
						lastRenderViewportRotation = _renderViewport.transform.localRotation.eulerAngles;
						lastRenderViewportPosition = _renderViewport.transform.position;
						UpdateViewportObjectsVisibility();
					}
					if (VGOBuoyancyAmplitude > 0)
						UpdateViewportObjectsBuoyancy();
				}
			}

			if (_showGrid)
				GridUpdateHighlightFade(); // Fades current selection
		}

		private int fixedFrame;

		private void FixedUpdate()
		{
			if (Time.frameCount != fixedFrame)
			{
				fixedFrame = Time.frameCount;
				UpdateViewportObjectsLoop();
			}
		}

		private void LateUpdate()
		{
			updateDoneThisFrame = false;

			if (renderViewPortIsTerrain)
			{
				if (_enableFreeCamera || !Application.isPlaying)
					SyncMapperCamWithMainCamera(); // catch any camera change between Update and LateUpdate
				SyncMainCameraWithMapperCam();
			}

			if (_earthBumpEnabled && _earthStyle.supportsBumpMap() && _sun != null)
				earthMat.SetVector("_SunLightDirection", -_sun.transform.forward);

			if (_showTiles)
				LateUpdateTiles();

			FitViewportToUIPanel();

			if (!paused)
				time += Time.deltaTime * timeSpeed;
		}

		private void UpdateShadersLOD()
		{
			if (isMiniMap)
				return;
			if (frontiersMat != null)
			{
				if (_thickerFrontiers)
				{
					var fw = _frontiersWidth;
					if (_frontiersDynamicWidth && !renderViewPortIsTerrain)
					{
						frontiersMat.shader.maximumLOD = lastDistanceFromCamera < 25f ? 100 : 300;
						if (lastDistanceFromCamera > 20f && lastDistanceFromCamera < 25f)
						{
							// smooth transition
							var t = 1f - (lastDistanceFromCamera - 20f) / 5f;
							fw = Mathf.Max(0.05f, _frontiersWidth * t);
						}
					}
					else
						frontiersMat.shader.maximumLOD = 100;
					frontiersMat.SetFloat("_Thickness", fw);
				}
				else
				{
					var lod = 300;
					if (_frontiersDynamicWidth && !renderViewPortIsTerrain)
						lod = lastDistanceFromCamera < 4.472f ? 100 :
							lastDistanceFromCamera < 17.888f ? 200 : 300;
					frontiersMat.shader.maximumLOD = lod;
				}
			}

			// Provinces
			if (provincesMat != null)
				provincesMat.shader.maximumLOD = lastDistanceFromCamera < 14.0 ? 200 : 300;
		}

		private Vector3 ClampDistanceToMap(Vector3 prevPos, Vector3 currPos, float currDistance,
			float clampDistance)
		{
			var plane = new Plane(transform.forward, transform.position);
			var prevDistance = Mathf.Abs(plane.GetDistanceToPoint(prevPos));

			var ta = (clampDistance - currDistance) / (prevDistance - currDistance);
			return Vector3.Lerp(currPos, prevPos, ta);
		}

		private void CheckRectConstraints()
		{
			if (isMiniMap)
				return;

			var cam = currentCamera;
			if (cam == null)
				return;

			if (_fitWindowHeight)
			{
				var distance = GetFrustumDistance();
				var camDist = GetCameraDistance();
				if (camDist > distance)
					cam.transform.position += cam.transform.forward * (camDist - distance);
			}

			float limitLeft, limitRight;
			if (_fitWindowWidth)
			{
				limitLeft = 0f;
				limitRight = 1f;
			}
			else
			{
				limitLeft = 0.9f;
				limitRight = 0.1f;
			}

			// Reduce floating-point errors
			Vector3 pos, apos = transform.position;
			if (renderViewportIsEnabled)
			{
				transform.position -= apos;
				cam.transform.position -= apos;
			}

			Vector3 posEdge;
			if (!_wrapHorizontally || renderViewPortIsTerrain)
			{
				// Clamp right
				posEdge = transform.TransformPoint(_windowRect.xMax, 0, 0);
				pos = cam.WorldToViewportPoint(posEdge);
				if (pos.x < limitRight)
				{
					pos.x = limitRight;
					pos = cam.ViewportToWorldPoint(pos);
					cam.transform.position += posEdge - pos;
					dragDampingStart = 0;
				}
				else
				{
					// Clamp left
					posEdge = transform.TransformPoint(_windowRect.xMin, 0, 0);
					pos = cam.WorldToViewportPoint(posEdge);
					if (pos.x > limitLeft)
					{
						pos.x = limitLeft;
						pos = cam.ViewportToWorldPoint(pos);
						cam.transform.position += posEdge - pos;
						dragDampingStart = 0;
					}
				}
			}

			float limitTop, limitBottom;
			if (_fitWindowHeight)
			{
				limitTop = 1.0f;
				limitBottom = 0f;
			}
			else
			{
				limitTop = 0.1f;
				limitBottom = 0.9f;
			}

			// Clamp top
			posEdge = transform.TransformPoint(0, _windowRect.yMax, 0);
			pos = cam.WorldToViewportPoint(posEdge);
			if (pos.y < limitTop)
			{
				pos.y = limitTop;
				pos = cam.ViewportToWorldPoint(pos);
				cam.transform.position += posEdge - pos;
				dragDampingStart = 0;
			}
			else
			{
				// Clamp bottom
				posEdge = transform.TransformPoint(0, _windowRect.yMin, 0);
				pos = cam.WorldToViewportPoint(posEdge);
				if (pos.y > limitBottom)
				{
					pos.y = limitBottom;
					pos = cam.ViewportToWorldPoint(pos);
					cam.transform.position += posEdge - pos;
					dragDampingStart = 0;
				}
			}

			// Reduce floating-point errors
			if (renderViewportIsEnabled)
			{
				transform.position += apos;
				cam.transform.position += apos;
			}
		}

		/// <summary>
		/// Check controls (keys, mouse, ...) and react
		/// </summary>
		private void PerformUserInteraction()
		{
			var deltaTime = Time.deltaTime * 60f;

			var buttonLeftPressed = Input.GetMouseButton(0) ||
			                        Input.touchSupported &&
			                        Input.touchCount == 1 &&
			                        Input.touches[0].phase != TouchPhase.Ended;
			if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
				clickTime = Time.time;

			// Use mouse wheel to zoom in and out
			if (!flyToActive && allowUserZoom && (mouseIsOver || wheelAccel != 0))
			{
				var wheel = Input.GetAxis("Mouse ScrollWheel");
				if (wheel != 0)
				{
					zoomDampingStart = Time.time;
					wheelAccel += wheel * (_invertZoomDirection ? -1 : 1);
				}

				Vector3 zoomCenter;
				// Support for pinch on mobile
				if (Input.touchSupported && Input.touchCount == 2)
				{
					// Store both touches.
					var touchZero = Input.GetTouch(0);
					var touchOne = Input.GetTouch(1);

					zoomCenter = (touchZero.position + touchOne.position) * 0.5f;

					// Find the position in the previous frame of each touch.
					var touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
					var touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

					// Find the magnitude of the vector (the distance) between the touches in each frame.
					var prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
					var touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

					// Find the difference in the distances between each frame.
					var deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

					// Pass the delta to the wheel accel
					if (deltaMagnitudeDiff != 0)
					{
						zoomDampingStart = Time.time;
						wheelAccel += deltaMagnitudeDiff;
					}

					pinching = true;
					dragDampingStart = 0;
				}
				else
					zoomCenter = Input.mousePosition;

				if (wheelAccel != 0)
				{
					wheelAccel = Mathf.Clamp(wheelAccel, -0.1f, 0.1f);
					if (wheelAccel >= 0.001f || wheelAccel <= -0.001f)
					{
						Vector3 dest;
						if (GetLocalHitFromScreenPos(zoomCenter, out dest, true))
							dest = transform.TransformPoint(dest);
						else
							dest = transform.position;

						if (_currentCamera.orthographic)
							_currentCamera.orthographicSize += _currentCamera.orthographicSize *
							                                   wheelAccel *
							                                   _mouseWheelSensitivity *
							                                   deltaTime;
						else
							_currentCamera.transform.Translate(
								(_currentCamera.transform.position - dest) *
								wheelAccel *
								_mouseWheelSensitivity *
								deltaTime, Space.World);
						if (zoomDampingStart > 0)
						{
							var t = (Time.time - zoomDampingStart) / _zoomDampingDuration;
							wheelAccel = Mathf.Lerp(wheelAccel, 0, t);
						}
						else
							wheelAccel = 0;
					}
					else
						wheelAccel = 0;
				}
			}

			if (pinching && wheelAccel == 0 || Input.touchCount == 0)
				pinching = false;

			// Verify if mouse enter a country boundary - we only check if mouse is inside the map
			if (!pinching)
			{
				if (mouseIsOver)
				{
					// Remember the last element clicked
					var buttonIndex = -1;
					var releasing = true;
					if (Input.GetMouseButtonUp(0))
					{
						buttonIndex = 0;
						if (!hasDragged)
							dragging = false;
					}
					else if (Input.GetMouseButtonUp(1))
						buttonIndex = 1;
					else if (Input.GetMouseButtonDown(0))
					{
						buttonIndex = 0;
						releasing = false;
					}
					else if (Input.GetMouseButtonDown(1))
					{
						buttonIndex = 1;
						releasing = false;
					}

					// Check highlighting only if flyTo is not active to prevent hiccups during movement
					if (!flyToActive)
					{
						Vector3 localPoint;
						var goodHit = GetLocalHitFromMousePos(out localPoint);
						var positionMoved = false;
						if (localPoint.x != lastMouseMapHitPos.x ||
						    localPoint.y != lastMouseMapHitPos.y ||
						    !lastMouseMapHitPosGood)
						{
							lastMouseMapHitPos = localPoint;
							lastMouseMapHitPosGood = goodHit;
							positionMoved = true;
						}
						if (Application.isMobilePlatform)
						{
							if (Input.touchCount == 1)
								if (!dragging && releasing)
								{
									CheckMousePos();
									GridCheckMousePos(); // Verify if mouse enter a territory boundary - we only check if mouse is inside the sphere of world
								}
						}
						else
						{
							if (positionMoved)
								if (!dragging)
								{
									CheckMousePos();
									GridCheckMousePos(); // Verify if mouse enter a territory boundary - we only check if mouse is inside the sphere of world
								}
						}
						if (positionMoved && lastMouseMapHitPosGood)
						{
							// Cursor follow
							if (_cursorFollowMouse)
								cursorLocation = lastMouseMapHitPos;

							// Raise mouse move event
							if (OnMouseMove != null)
								OnMouseMove(lastMouseMapHitPos.x, lastMouseMapHitPos.y);
						}
					}

					if (buttonIndex >= 0)
					{
						if (releasing)
						{
							if (OnMouseRelease != null)
								OnMouseRelease(_cursorLocation.x, _cursorLocation.y, buttonIndex);
							if (!hasDragged || Time.time - clickTime < TAP_THRESHOLD)
							{
								_countryLastClicked = _countryHighlightedIndex;
								_countryRegionLastClicked = _countryRegionHighlightedIndex;
								_provinceLastClicked = _provinceHighlightedIndex;
								_provinceRegionLastClicked = _provinceRegionHighlightedIndex;
								_cityLastClicked = _cityHighlightedIndex;
								_cellLastClickedIndex = _cellHighlightedIndex;
								if (VGOLastHighlighted == null || !VGOLastHighlighted.blocksRayCast)
								{
									if (_countryLastClicked >= 0)
									{
										if (OnCountryClick != null)
											OnCountryClick(_countryLastClicked, _countryRegionLastClicked,
												buttonIndex);
										if (OnRegionClick != null)
											OnRegionClick(
												_countries[_countryLastClicked]
													.regions[_countryRegionLastClicked], buttonIndex);
									}
									if (_provinceLastClicked >= 0)
									{
										if (OnProvinceClick != null)
											OnProvinceClick(_provinceLastClicked,
												_provinceRegionLastClicked, buttonIndex);
										if (OnRegionClick != null)
											OnRegionClick(
												_provinces[_provinceLastClicked]
													.regions[_provinceRegionLastClicked], buttonIndex);
									}
									if (_cityLastClicked >= 0 && OnCityClick != null)
										OnCityClick(_cityLastClicked, buttonIndex);
									if (_cellLastClickedIndex >= 0 && OnCellClick != null)
										OnCellClick(_cellLastClickedIndex, buttonIndex);
									if (OnClick != null)
										OnClick(_cursorLocation.x, _cursorLocation.y, buttonIndex);
								}
							}
						}
						else
						{
							if (OnMouseDown != null)
								OnMouseDown(_cursorLocation.x, _cursorLocation.y, buttonIndex);
						}
					}

					if (hasDragged && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)))
						if (OnDragEnd != null)
							OnDragEnd();

					// if mouse/finger is over map, implement drag and zoom of the world
					if (_allowUserDrag)
					{
						// Use left mouse button to drag the map
						if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
						{
							mouseDragStart = Input.mousePosition;
							mouseDragStartHitPos = lastMouseMapHitPos;
							dragging = true;
							hasDragged = false;
							flyToActive = false;
						}

						// Use right mouse button and fly and center on target country
						if (Input.GetMouseButtonDown(1) &&
						    !Input
							    .touchSupported) // two fingers can be interpreted as right mouse button -> prevent this.
							if (Input.GetMouseButtonDown(1) && _centerOnRightClick)
							{
								if (VGOLastHighlighted != null)
									FlyToLocation(VGOLastHighlighted.currentMap2DLocation, 0.8f);
								else if (_provinceHighlightedIndex >= 0)
								{
									var regionPos = provinces[_provinceHighlightedIndex]
										.regions[_provinceRegionHighlightedIndex].center;
									FlyToLocation(regionPos, 0.8f);
								}
								else if (_countryHighlightedIndex >= 0)
								{
									var regionPos = _countries[_countryHighlightedIndex]
										.regions[_countryRegionHighlightedIndex].center;
									FlyToLocation(regionPos, 0.8f);
								}
							}
					}
				}

				// Check special keys
				if (_allowUserKeys)
				{
					var keyDragDirection = Misc.Vector3zero;
					var pressed = false;
					if (Input.GetKey(_keyUp))
					{
						keyDragDirection = Misc.Vector3down;
						pressed = true;
					}
					if (Input.GetKey(_keyDown))
					{
						keyDragDirection += Misc.Vector3up;
						pressed = true;
					}
					if (Input.GetKey(_keyLeft))
					{
						keyDragDirection += Misc.Vector3right;
						pressed = true;
					}
					if (Input.GetKey(_keyRight))
					{
						keyDragDirection += Misc.Vector3left;
						pressed = true;
					}
					if (pressed)
					{
						buttonLeftPressed = false;
						dragDirection = keyDragDirection;
						if (_currentCamera.orthographic)
							dragSpeed = _currentCamera.orthographicSize * 10.0f * _mouseDragSensitivity;
						else
							dragSpeed = lastDistanceFromCamera * _mouseDragSensitivity;
						dragDirection *= _dragKeySpeedMultiplier * dragSpeed;
						if (dragFlipDirection)
							dragDirection *= -1;
						if (_dragConstantSpeed)
							dragDampingStart = Time.time + _dragDampingDuration;
						else
							dragDampingStart = Time.time;
					}
				}

				if (dragging)
				{
					if (buttonLeftPressed)
					{
						if (_allowUserDrag || _allowUserKeys)
						{
							if (_dragConstantSpeed)
							{
								if (lastMouseMapHitPosGood && mouseIsOver)
								{
									dragDirection = lastMouseMapHitPos - mouseDragStartHitPos;
									dragDirection.x = ApplyDragThreshold(dragDirection.x);
									dragDirection.y = ApplyDragThreshold(dragDirection.y);
									if (dragDirection.x != 0 || dragDirection.y != 0)
									{
										dragDirection = transform.TransformVector(dragDirection);
										_currentCamera.transform.Translate(-dragDirection, Space.World);
										dragDampingStart = Time.time;
									}
								}
							}
							else
							{
								dragDirection = Input.mousePosition - mouseDragStart;
								dragDirection.x = ApplyDragThreshold(dragDirection.x);
								dragDirection.y = ApplyDragThreshold(dragDirection.y);
								if (dragDirection.x != 0 || dragDirection.y != 0)
								{
									if (_currentCamera.orthographic)
										dragSpeed = _currentCamera.orthographicSize *
										            _mouseDragSensitivity *
										            0.00035f;
									else
										dragSpeed = lastDistanceFromCamera *
										            _mouseDragSensitivity *
										            0.00035f;
									dragDampingStart = Time.time;
									dragDirection *= dragSpeed;
									// Drag along the map plane
									_currentCamera.transform.Translate(-dragDirection * deltaTime,
										transform);
								}
							}
						}
					}
					else
						dragging = false;
				}

				if (!hasDragged)
				{
					if (buttonLeftPressed &&
					    Time.time - clickTime > TAP_THRESHOLD &&
					    (dragDirection.x != 0 || dragDirection.y != 0))
					{
						hasDragged = true;
						if (OnDragStart != null)
							OnDragStart();
					}
				}
				else if (!buttonLeftPressed)
					hasDragged = false;
			}

			// Check scroll on borders
			if (_allowScrollOnScreenEdges && (_allowUserDrag || _allowUserKeys))
			{
				var onEdge = false;
				var mx = Input.mousePosition.x;
				var my = Input.mousePosition.y;
				if (mx >= 0 && mx < Screen.width && my >= 0 && my < Screen.height)
				{
					if (my < _screenEdgeThickness)
					{
						dragDirection = Misc.Vector3up;
						onEdge = true;
					}
					if (my >= Screen.height - _screenEdgeThickness)
					{
						dragDirection = Misc.Vector3down;
						onEdge = true;
					}
					if (mx < _screenEdgeThickness)
					{
						dragDirection = Misc.Vector3right;
						onEdge = true;
					}
					if (mx >= Screen.width - _screenEdgeThickness)
					{
						dragDirection = Misc.Vector3left;
						onEdge = true;
					}
				}
				if (onEdge)
				{
					if (_currentCamera.orthographic)
						dragSpeed = _currentCamera.orthographicSize * 10.0f * _mouseDragSensitivity;
					else
						dragSpeed = lastDistanceFromCamera * _mouseDragSensitivity;
					dragDirection *= 0.1f * dragSpeed;
					if (dragFlipDirection)
						dragDirection *= -1;
					dragDampingStart = Time.time;
				}
			}

			if (dragDampingStart > 0 && !buttonLeftPressed)
			{
				var t = 1f - (Time.time - dragDampingStart) / dragDampingDuration;
				if (t < 0)
				{
					t = 0;
					dragDampingStart = 0f;
				}
				else if (t > 1f)
				{
					t = 1f;
					dragDampingStart = 0f;
				}
				dragging = true;
				_currentCamera.transform.Translate(-dragDirection * (t * deltaTime), transform);
			}
		}

		public void OnMouseEnter()
		{
			mouseIsOver = true;
		}

		public void OnMouseExit()
		{
			// Make sure it's outside of map
			var mousePos = Input.mousePosition;
			if (mousePos.x >= 0 &&
			    mousePos.x < Screen.width &&
			    mousePos.y >= 0 &&
			    mousePos.y < Screen.height)
			{
				var ray = cameraMain.ScreenPointToRay(mousePos);
				var hitCount = Physics.RaycastNonAlloc(ray.origin, ray.direction, tempHits, 2000);
				for (var k = 0; k < hitCount; k++)
					if (tempHits[k].collider.name.Equals(WMSKMiniMap.MINIMAP_NAME))
					{
						mouseIsOver = false;
						return;
					}
				for (var k = 0; k < hitCount; k++)
					if (tempHits[k].collider.gameObject == _renderViewport)
						return;
			}

			mouseIsOver = false;
			HideCountryRegionHighlight();
		}

		public void DoOnMouseClick()
		{
			mouseIsOver = true;
			Update();
		}

		public void DoOnMouseRelease()
		{
			Update();
			mouseIsOver = false;
			HideCountryRegionHighlight();
		}

		#endregion

		#region System initialization

		public void Init()
		{
#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
			var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
			if (prefabInstanceStatus != PrefabInstanceStatus.NotAPrefab &&
			    prefabInstanceStatus != PrefabInstanceStatus.Disconnected)
				PrefabUtility.UnpackPrefabInstance(gameObject,
					PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
#else
            UnityEditor.PrefabType prefabType = UnityEditor.PrefabUtility.GetPrefabType(gameObject);
            if (prefabType != UnityEditor.PrefabType.None && prefabType != UnityEditor.PrefabType.DisconnectedPrefabInstance && prefabType != UnityEditor.PrefabType.DisconnectedModelPrefabInstance) {
                UnityEditor.PrefabUtility.DisconnectPrefabInstance(gameObject);
            }
#endif
#endif

			if (disposalManager == null)
				disposalManager = new DisposalManager();

			// Conversion from old scales
			if (_renderViewportGOAutoScaleMultiplier > 10f)
			{
				_renderViewportGOAutoScaleMultiplier *= 0.01f;
				isDirty = true;
			}

			// Boot initialization
			tempHits = new RaycastHit[100];
			var mapLayer = gameObject.layer;
			foreach (Transform t in transform)
				t.gameObject.layer = mapLayer;
			var rb = GetComponent<Rigidbody>();
			if (rb != null)
				rb.detectCollisions = false;

			SetupVGOs();

			SetupViewport();

			// Labels materials
			ReloadFont();
			ReloadProvinceFont();

			// Map materials
			frontiersMat = Instantiate(Resources.Load<Material>("WMSK/Materials/Frontiers"));
			if (disposalManager != null)
				disposalManager
					.MarkForDisposal(frontiersMat); // frontiersMat.hideFlags = HideFlags.DontSave;
			frontiersMat.shader.maximumLOD = 300;
			hudMatCountry = Instantiate(Resources.Load<Material>("WMSK/Materials/HudCountry"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(hudMatCountry); //.hideFlags = HideFlags.DontSave;
			hudMatProvince = Instantiate(Resources.Load<Material>("WMSK/Materials/HudProvince"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(hudMatProvince); //.hideFlags = HideFlags.DontSave;
			hudMatProvince.renderQueue++; // render on top of country highlight
			CheckCityIcons();
			citiesNormalMat = Instantiate(Resources.Load<Material>("WMSK/Materials/Cities"));
			citiesNormalMat.name = "Cities";
			if (disposalManager != null)
				disposalManager.MarkForDisposal(citiesNormalMat); //.hideFlags = HideFlags.DontSave;
			citiesRegionCapitalMat =
				Instantiate(Resources.Load<Material>("WMSK/Materials/CitiesCapitalRegion"));
			citiesRegionCapitalMat.name = "CitiesCapitalRegion";
			if (disposalManager != null)
				disposalManager.MarkForDisposal(citiesRegionCapitalMat); //.hideFlags = HideFlags.DontSave;
			citiesCountryCapitalMat =
				Instantiate(Resources.Load<Material>("WMSK/Materials/CitiesCapitalCountry"));
			citiesCountryCapitalMat.name = "CitiesCapitalCountry";
			if (disposalManager != null)
				disposalManager.MarkForDisposal(
					citiesCountryCapitalMat); //.hideFlags = HideFlags.DontSave;

			provincesMat = Instantiate(Resources.Load<Material>("WMSK/Materials/Provinces"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(provincesMat); //.hideFlags = HideFlags.DontSave;
			provincesMat.shader.maximumLOD = 300;
			outlineMatSimple = Instantiate(Resources.Load<Material>("WMSK/Materials/Outline"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(outlineMatSimple); //.hideFlags = HideFlags.DontSave;
			outlineMatTextured = Instantiate(Resources.Load<Material>("WMSK/Materials/OutlineTex"));
			if (_outlineTexture == null)
				_outlineTexture = (Texture2D)outlineMatTextured.mainTexture;
			outlineMatTextured.mainTexture = _outlineTexture;
			outlineMatTextured.mainTextureScale = new Vector2(_outlineTilingScale, 1f);
			if (disposalManager != null)
				disposalManager.MarkForDisposal(outlineMatTextured); //.hideFlags = HideFlags.DontSave;
			coloredMat = Instantiate(Resources.Load<Material>("WMSK/Materials/ColorizedRegion"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(coloredMat); //.hideFlags = HideFlags.DontSave;
			coloredAlphaMat =
				Instantiate(Resources.Load<Material>("WMSK/Materials/ColorizedTranspRegion"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(coloredAlphaMat); //.hideFlags = HideFlags.DontSave;
			texturizedMat = Instantiate(Resources.Load<Material>("WMSK/Materials/TexturizedRegion"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(texturizedMat); //.hideFlags = HideFlags.DontSave;
			cursorMatH = Instantiate(Resources.Load<Material>("WMSK/Materials/CursorH"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(cursorMatH); // = HideFlags.DontSave;
			cursorMatV = Instantiate(Resources.Load<Material>("WMSK/Materials/CursorV"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(cursorMatV); //.hideFlags = HideFlags.DontSave;
			imaginaryLinesMat = Instantiate(Resources.Load<Material>("WMSK/Materials/ImaginaryLines"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(imaginaryLinesMat); //.hideFlags = HideFlags.DontSave;
			markerLineMat = Instantiate(Resources.Load<Material>("WMSK/Materials/MarkerLine"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(markerLineMat); //.hideFlags = HideFlags.DontSave;
			markerMat =
				Instantiate(
					markerLineMat); //Resources.Load<Material>("WMSK/Materials/Marker")); // Marker shader is not compatible with LWRP so we use markerLineMat which serves the same purpose. Kept old shader for a while for compatibility reasons.
			if (disposalManager != null)
				disposalManager.MarkForDisposal(markerMat);
			mountPointSpot = Resources.Load<GameObject>("WMSK/Prefabs/MountPointSpot");
			mountPointsMat = Instantiate(Resources.Load<Material>("WMSK/Materials/Mount Points"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(mountPointsMat); //.hideFlags = HideFlags.DontSave;
			gridMat = Instantiate(Resources.Load<Material>("WMSK/Materials/Grid"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(gridMat); //.hideFlags = HideFlags.DontSave;
			gridMat.renderQueue++;
			hudMatCell = Instantiate(Resources.Load<Material>("WMSK/Materials/HudCell"));
			if (disposalManager != null)
				disposalManager.MarkForDisposal(hudMatCell); //.hideFlags = HideFlags.DontSave;
			hudMatCell.renderQueue++;
			extrudedMat = Instantiate(Resources.Load<Material>("WMSK/Materials/ExtrudedRegion"));
			SRP.Configure(extrudedMat);

			coloredMatCache = new Dictionary<Color, Material>();
			markerMatCache = new Dictionary<Color, Material>();

			if (_dontLoadGeodataAtStart)
			{
				countries = new Country[0];
				provinces = new Province[0];
				cities = new City[0];
				mountPoints = new List<MountPoint>();
			}
			else
				ReloadData();

			if (_showTiles)
				InitTileSystem();

			// Redraw frontiers and cities -- destroy layers if they already exists
			if (!Application.isPlaying)
				Redraw();

			PostInit();
		}

		private void PostInit()
		{
			// Additional setup executed only during initialization

			// Check material
			Renderer renderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
			if (renderer.sharedMaterial == null)
				RestyleEarth();

			if (hudMatCountry != null && hudMatCountry.color != _fillColor)
				hudMatCountry.color = _fillColor;
			UpdateFrontiersMaterial();
			if (hudMatProvince != null && hudMatProvince.color != _provincesFillColor)
				hudMatProvince.color = _provincesFillColor;
			if (provincesMat != null && provincesMat.color != _provincesColor)
				provincesMat.color = _provincesColor;
			if (citiesNormalMat.color != _citiesColor)
				citiesNormalMat.color = _citiesColor;
			if (citiesRegionCapitalMat.color != _citiesRegionCapitalColor)
				citiesRegionCapitalMat.color = _citiesRegionCapitalColor;
			if (citiesCountryCapitalMat.color != _citiesCountryCapitalColor)
				citiesCountryCapitalMat.color = _citiesCountryCapitalColor;
			if (outlineMat.color != _outlineColor)
				outlineMat.color = _outlineColor;
			if (cursorMatH.color != _cursorColor)
				cursorMatH.color = _cursorColor;
			if (cursorMatV.color != _cursorColor)
				cursorMatV.color = _cursorColor;
			if (imaginaryLinesMat.color != _imaginaryLinesColor)
				imaginaryLinesMat.color = _imaginaryLinesColor;
			if (hudMatCell != null && hudMatCell.color != _cellHighlightColor)
				hudMatCell.color = _cellHighlightColor;
			if (gridMat != null && gridMat.color != _gridColor)
				gridMat.color = _gridColor;
			if (_enableCellHighlight)
				showLatitudeLines = showLongitudeLines = false;

			// Unity 5.3.1 prevents raycasting in the scene view if rigidbody is present - we have to delete it in editor time but re-enable it here during play mode
			if (Application.isPlaying)
			{
				if (GetComponent<Rigidbody>() == null)
				{
					var rb = gameObject.AddComponent<Rigidbody>();
					rb.useGravity = false;
					rb.isKinematic = true;
				}

				Redraw();

				if (_prewarm)
				{
					CountriesPrewarmBigSurfaces();
					PathFindingPrewarm();
				}
			}

			maxFrustumDistance = float.MaxValue;
			if (_fitWindowWidth || _fitWindowHeight)
				CenterMap();
		}

		/// <summary>
		/// Reloads the data of frontiers and cities from datafiles and redraws the map.
		/// </summary>
		public void ReloadData()
		{
			// Destroy surfaces layer
			DestroySurfaces();
			// read precomputed data
			ReadCountriesPackedString();
			if (_showCities || GetComponent<WMSK_Editor>() != null)
				ReadCitiesPackedString();
			if (_showProvinces || _enableProvinceHighlight || GetComponent<WMSK_Editor>() != null)
				ReadProvincesPackedString();
			ReadMountPointsPackedString();
		}

		private void DestroySurfaces()
		{
			HideCountryRegionHighlights(true);
			HideProvinceRegionHighlight();
			if (frontiersCacheHit != null)
				frontiersCacheHit.Clear();
			InitSurfacesCache();
			if (_surfacesLayer != null)
				DestroyImmediate(_surfacesLayer);
			if (provincesObj != null)
				DestroyImmediate(provincesObj);
		}

		#endregion

		#region Drawing stuff

		private float lastGCTime;

		/// <summary>
		/// Immediately destroys any gameobject and its children including dynamically created meshes
		/// </summary>
		/// <param name="go">Go.</param>
		private void DestroyRecursive(GameObject go)
		{
			if (go == null)
				return;
			var mm = go.GetComponentsInChildren<MeshFilter>(true);
			for (var k = 0; k < mm.Length; k++)
				if (mm[k] != null)
				{
					var mesh = mm[k].sharedMesh;
					mesh.Clear(false);
					DestroyImmediate(mesh);
					mm[k].sharedMesh = null;
				}
			var gg = go.GetComponentsInChildren<Transform>(true);
			for (var k = 0; k < gg.Length; k++)
				if (gg[k] != null && gg[k] != go.transform)
				{
					DestroyImmediate(gg[k].gameObject);
					gg[k] = null;
				}
			DestroyImmediate(go);

			if (!Application.isPlaying || Time.time - lastGCTime > 10f)
			{
				lastGCTime = Time.time;
				Resources.UnloadUnusedAssets();
				GC.Collect();
			}
		}

		/// <summary>
		/// Convenient function to organize any surface or outline into the hierarchy
		/// </summary>
		/// <param name="entityId">Entity identifier.</param>
		/// <param name="categoryName">Category name.</param>
		/// <param name="obj">Object.</param>
		private void ParentObjectToRegion(string entityId, string categoryName, GameObject obj)
		{
			var entityRoot = surfacesLayer.transform.Find(entityId);
			if (entityRoot == null)
			{
				var aux = new GameObject(entityId);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(aux);
				aux.hideFlags |= HideFlags.HideInHierarchy;
				entityRoot = aux.transform;
				entityRoot.SetParent(surfacesLayer.transform, false);
			}
			// Check if childNode exists
			Transform category;
			if (string.IsNullOrEmpty(categoryName))
				category = entityRoot;
			else
			{
				category = entityRoot.Find(categoryName);
				if (category == null)
				{
					var aux = new GameObject(categoryName);
					if (disposalManager != null)
						disposalManager.MarkForDisposal(aux);
					aux.hideFlags |= HideFlags.HideInHierarchy;
					category = aux.transform;
					category.SetParent(entityRoot, false);
				}
			}
			// Check if object already exists
			var t = category.Find(obj.name);
			if (t != null)
				DestroyImmediate(t.gameObject);
			obj.transform.SetParent(category, false);
			obj.layer = gameObject.layer;
		}

		/// <summary>
		/// Convenient function which destroys a previously created surface or outline related to a region
		/// </summary>
		/// <param name="entityId">Entity identifier.</param>
		/// <param name="categoryName">Category name.</param>
		/// <param name="objectName">Object name.</param>
		private void HideRegionObject(string entityId, string categoryName, string objectName)
		{
			var t = surfacesLayer.transform.Find(entityId);
			if (t == null)
				return;
			if (string.IsNullOrEmpty(categoryName))
				t = t.transform.Find(objectName);
			else
				t = t.transform.Find(categoryName + "/" + objectName);
			if (t != null)
				t.gameObject.SetActive(false);
		}

		/// <summary>
		/// Used internally and by other components to redraw the layers in specific moments. You shouldn't call this method directly.
		/// </summary>
		/// <param name="forceReconstructFrontiers">If set to <c>true</c> frontiers will be recomputed.</param>
		public void Redraw(bool forceReconstructFrontiers)
		{
			if (forceReconstructFrontiers)
				needOptimizeFrontiers = true;
			Redraw();
		}

		/// <summary>
		/// Redraws all map layers.
		/// </summary>
		public void Redraw()
		{
			if (!gameObject.activeInHierarchy)
				return;

			if (lastDistanceFromCamera == 0)
			{
				var cam = currentCamera;
				if (cam == null)
					return;
				lastDistanceFromCamera = (transform.position - cam.transform.position).magnitude;
			}

			shouldCheckBoundaries = true;

			DestroySurfaces(); // Initialize surface cache, destroys already generated surfaces

			RestyleEarth(); // Apply texture to Earth

			DrawFrontiers(); // Redraw frontiers -- the next method is also called from width property when this is changed

			DrawAllProvinceBorders(needOptimizeFrontiers, false); // Redraw province borders

			needOptimizeFrontiers = false;

			DrawCities(); // Redraw cities layer

			DrawMountPoints(); // Redraw mount points (only in Editor time)

			DrawCursor(); // Draw cursor lines

			DrawImaginaryLines(); // Draw longitude & latitude lines

			DrawMapLabels(); // Destroy existing texts and draw them again

			DrawGrid();

			SetupViewport();
		}

		private void InitSurfacesCache()
		{
			if (surfaces != null)
			{
				var cached = new List<GameObject>(surfaces.Values);
				for (var k = 0; k < cached.Count; k++)
					if (cached[k] != null)
						DestroyImmediate(cached[k]);
				surfaces.Clear();
			}
			else
				surfaces = new Dictionary<int, GameObject>();
		}

		private void CreateSurfacesLayer()
		{
			var t = transform.Find(SURFACE_LAYER);
			if (t != null)
			{
				DestroyImmediate(t.gameObject);
				for (var k = 0; k < _countries.Length; k++)
					for (var r = 0; r < _countries[k].regions.Count; r++)
						_countries[k].regions[r].customMaterial = null;
			}
			_surfacesLayer = new GameObject(SURFACE_LAYER);
			_surfacesLayer.transform.SetParent(transform, false);
			_surfacesLayer.transform.localPosition = Misc.Vector3back * 0.001f;
			_surfacesLayer.layer = gameObject.layer;
		}

		private void RestyleEarth()
		{
			if (gameObject == null)
				return;

			string materialName;
			switch (_earthStyle)
			{
				case EARTH_STYLE.Alternate1:
					materialName = "Earth2";
					break;
				case EARTH_STYLE.Alternate2:
					materialName = "Earth4";
					break;
				case EARTH_STYLE.Alternate3:
					materialName = "Earth5";
					break;
				case EARTH_STYLE.SolidColor:
					materialName = "EarthSolidColor";
					break;
				case EARTH_STYLE.Texture:
					materialName = "EarthTexture";
					break;
				case EARTH_STYLE.NaturalHighRes:
					materialName = "EarthHighRes";
					break;
				case EARTH_STYLE.NaturalHighRes16K:
					materialName = "EarthHighRes16K";
					break;
				case EARTH_STYLE.NaturalScenic:
					materialName = "EarthScenic";
					break;
				case EARTH_STYLE.NaturalScenicPlus:
					materialName = "EarthScenicPlus";
					break;
				case EARTH_STYLE.NaturalScenicPlusAlternate1:
					materialName = "EarthScenicPlusAlternate1";
					break;
				case EARTH_STYLE.NaturalScenicPlus16K:
					materialName = "EarthScenicPlus16K";
					break;
				default:
					materialName = "Earth";
					break;
			}

			var renderer = GetComponent<MeshRenderer>();
			if (earthMat == null ||
			    renderer.sharedMaterial == null ||
			    !renderer.sharedMaterial.name.Equals(materialName))
			{
				earthMat = Instantiate(Resources.Load<Material>("WMSK/Materials/" + materialName));
				if (disposalManager != null)
					disposalManager.MarkForDisposal(earthMat);
				earthMat.name = materialName;
				renderer.material = earthMat;
				if (earthBlurred != null && RenderTexture.active != earthBlurred)
				{
					DestroyImmediate(earthBlurred);
					earthBlurred = null;
				}
			}

			if (_earthStyle == EARTH_STYLE.SolidColor)
				earthMat.color = _earthColor;
			else if (_earthStyle == EARTH_STYLE.Texture)
				earthMat.color = _earthColor;
			else if (_earthStyle.isScenicPlus())
			{
				earthMat.SetColor("_WaterColor", _waterColor);
				var waterInfo = new Vector3(_waterLevel, _waterFoamThreshold, _waterFoamIntensity);
				earthMat.SetVector("_WaterLevel", waterInfo);
				if (earthBlurred == null && _earthStyle != EARTH_STYLE.NaturalScenicPlus16K)
					EarthPrepareBlurredTexture();
				if (_waterMask != null)
					earthMat.SetTexture("_TerrestrialMap", _waterMask);
				UpdateScenicPlusDistance();
			}
			if (_earthTexture != null && earthMat.HasProperty("_MainTex"))
				earthMat.mainTexture = _earthTexture;
			earthMat.mainTextureOffset = -_earthTextureOffset;
			earthMat.mainTextureScale = new Vector2(1f / _earthTextureScale.x, 1f / _earthTextureScale.y);
			;

			if (_earthBumpMapTexture != null && _earthStyle.supportsBumpMap())
				earthMat.SetTexture("_NormalMap", _earthBumpMapTexture);

			if (_sun == null)
				FindDirectionalLight();
			if (_earthStyle.supportsBumpMap())
			{
				if (_earthBumpEnabled)
				{
					earthMat.EnableKeyword(SKW_BUMPMAP_ENABLED);
					earthMat.SetFloat("_BumpAmount", _earthBumpAmount);
					if (_sun != null)
						earthMat.SetVector("_SunLightDirection", -_sun.transform.forward);
					else
						earthMat.SetVector("_SunLightDirection", transform.forward);
				}
				else
					earthMat.DisableKeyword(SKW_BUMPMAP_ENABLED);
			}
			if (_pathFindingVisualizeMatrixCost &&
			    earthMat != null &&
			    pathFindingCustomMatrixCostTexture != null)
				earthMat.mainTexture = pathFindingCustomMatrixCostTexture;

			if (_showTiles)
			{
				if (Application.isPlaying)
				{
					if (tilesRoot == null)
						InitTileSystem();
					else
						ResetTiles();
					if (!_tileTransparentLayer)
						renderer.enabled = false;
				}
				else if (tilesRoot != null)
					tilesRoot.gameObject.SetActive(false);
			}
		}

		private void FindDirectionalLight()
		{
			var lights = FindObjectsOfType<Light>();
			if (lights == null)
				return;
			for (var k = 0; k < lights.Length; k++)
				if (lights[k] != null &&
				    lights[k].isActiveAndEnabled &&
				    lights[k].type == LightType.Directional)
				{
					_sun = lights[k].gameObject;
					return;
				}
		}

		private void EarthPrepareBlurredTexture()
		{
			var earthTex = (Texture2D)earthMat.GetTexture("_MainTex");
			if (earthTex == null)
				return;

			if (earthBlurred == null)
				earthBlurred = new RenderTexture(earthTex.width / 8, earthTex.height / 8, 0);
			var blurMat = new Material(Shader.Find("WMSK/Blur5Tap"));
			if (blurMat != null)
				Graphics.Blit(earthTex, earthBlurred, blurMat);
			else
				Graphics.Blit(earthTex, earthBlurred);
			earthMat.SetTexture("_EarthBlurred", earthBlurred);
		}

		#endregion

		#region Highlighting

		private bool GetLocalHitFromMousePos(out Vector3 localPoint)
		{
			var mousePos = Input.mousePosition;
			if (mousePos.x < 0 ||
			    mousePos.x >= Screen.width ||
			    mousePos.y < 0 ||
			    mousePos.y >= Screen.height)
			{
				localPoint = Misc.Vector3zero;
				return false;
			}
			return GetLocalHitFromScreenPos(mousePos, out localPoint, false);
		}

		private bool GetMapPosFromViewportPoint(ref Vector3 localPoint, bool nonWrap)
		{
			var tl = _currentCamera.WorldToViewportPoint(
				transform.TransformPoint(new Vector3(-0.5f, 0.5f)));
			var br = _currentCamera.WorldToViewportPoint(
				transform.TransformPoint(new Vector3(0.5f, -0.5f)));

			if (nonWrap)
			{
				localPoint.x = (localPoint.x - tl.x) / (br.x - tl.x) - 0.5f;
				localPoint.y = (localPoint.y - br.y) / (tl.y - br.y) - 0.5f;
				return true;
			}
			if (_wrapHorizontally)
			{
				// enables wrapping mode location
				if (localPoint.x < tl.x)
					localPoint.x = br.x - (tl.x - localPoint.x);
				else if (localPoint.x > br.x)
					localPoint.x = tl.x + localPoint.x - br.x;
			}
			// Trace the ray from this position in mapper cam space
			if (localPoint.x >= tl.x &&
			    localPoint.x <= br.x &&
			    localPoint.y >= br.y &&
			    localPoint.y <= tl.y)
			{
				localPoint.x = (localPoint.x - tl.x) / (br.x - tl.x) - 0.5f;
				localPoint.y = (localPoint.y - br.y) / (tl.y - br.y) - 0.5f;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Check mouse hit on the map and return the local plane coordinate. Handles viewports.
		/// </summary>
		/// <returns><c>true</c>, if local hit from screen position was gotten, <c>false</c> otherwise.</returns>
		/// <param name="screenPos">Screen position.</param>
		/// <param name="localPoint">Local point.</param>
		/// <param name="nonWrap">If true is passed, then a local hit is returned either on the real map plane or in a assumed wrapped plane next to it (effectively returning an x coordinate from -1.5..1.5</param>
		public bool GetLocalHitFromScreenPos(Vector3 screenPos, out Vector3 localPoint, bool nonWrap)
		{
			if (viewportMode == ViewportMode.MapPanel)
			{
				Vector2 pos;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(renderViewportUIPanel, screenPos,
					null, out pos);
				pos = Rect.PointToNormalized(renderViewportUIPanel.rect, pos);
				if (pos.x >= 0 && pos.x <= 1 && pos.y >= 0 && pos.y <= 1)
				{
					localPoint = pos;
					if (GetMapPosFromViewportPoint(ref localPoint, nonWrap))
						return true;
				}
				localPoint = Misc.Vector3zero;
				return false;
			}

			var ray = cameraMain.ScreenPointToRay(screenPos);
			var hitCount = Physics.RaycastNonAlloc(ray.origin, ray.direction, tempHits, 2000, layerMask);
			if (hitCount > 0)
				for (var k = 0; k < hitCount; k++) // Hit the map?
					if (tempHits[k].collider.gameObject == _renderViewport)
					{
						if (viewportMode == ViewportMode.Terrain)
						{
							localPoint = tempHits[k].point; // hit on terrain in world space
							var terrainCenterX = terrain.transform.position.x +
							                     terrain.terrainData.size.x * 0.5f;
							var terrainCenterZ = terrain.transform.position.z +
							                     terrain.terrainData.size.z * 0.5f;
							localPoint.x = (localPoint.x - terrainCenterX) / terrain.terrainData.size.x;
							localPoint.y = (localPoint.z - terrainCenterZ) / terrain.terrainData.size.z;
							localPoint.z = 0;
							return true;
						}
						localPoint = _renderViewport.transform.InverseTransformPoint(tempHits[k].point);
						localPoint.z = 0;
						// Is the viewport a render viewport or the map itself? If it's a render viewport projects hit into mapper cam space
						if (renderViewportIsEnabled)
						{
							// Get plane in screen space
							localPoint.x += 0.5f; // convert to viewport coordinates
							localPoint.y += 0.5f;
							if (GetMapPosFromViewportPoint(ref localPoint, nonWrap))
								return true;
						}
						else
							return true;
					}
			localPoint = Misc.Vector3zero;
			return false;
		}

		private void CheckMousePos()
		{
			if (!lastMouseMapHitPosGood)
			{
				HideCountryRegionHighlight();
				if (!_drawAllProvinces)
					HideProvinces();
				return;
			}

			// verify if hitPos is inside any country polygon
			var candidateCountryIndex = -1;
			var candidateCountryRegionIndex = -1;
			var candidateProvinceIndex = -1;
			var candidateProvinceRegionIndex = -1;
			var candidateProvinceRegionSize = float.MaxValue;

			// optimization: is inside current highlighted region?
			var needCheckRegion = _enableEnclaves ||
			                      (_showProvinces || _enableProvinceHighlight) &&
			                      _provinceHighlightedIndex < 0 ||
			                      _provinceRegionHighlightedIndex < 0 ||
			                      (_showFrontiers || _enableCountryHighlight) &&
			                      (_countryHighlightedIndex < 0 || _countryRegionHighlightedIndex < 0);

			if (!needCheckRegion)
			{
				if ((_showProvinces || _enableProvinceHighlight) &&
				    _provinceHighlightedIndex >= 0 &&
				    _provinceRegionHighlightedIndex >= 0 &&
				    !_provinces[provinceHighlightedIndex].regions[_provinceRegionHighlightedIndex]
					    .Contains(lastMouseMapHitPos))
					needCheckRegion = true;
				if (!needCheckRegion &&
				    (_showFrontiers || _enableCountryHighlight) &&
				    _countryHighlightedIndex >= 0 &&
				    _countryRegionHighlightedIndex >= 0 &&
				    !_countries[_countryHighlightedIndex].regions[_countryRegionHighlightedIndex]
					    .Contains(lastMouseMapHitPos))
					needCheckRegion = true;
			}

			if (needCheckRegion)
			{
				var countryCount = countriesOrderedBySize.Count;
				for (var oc = 0; oc < countryCount; oc++)
				{
					var c = _countriesOrderedBySize[oc];
					var country = _countries[c];
					if (country.hidden)
						continue;
					if (!country.regionsRect2D.Contains(lastMouseMapHitPos))
						continue;
					var regionCount = country.regions.Count;
					for (var cr = 0; cr < regionCount; cr++)
					{
						var countryRegion = country.regions[cr];
						if (countryRegion.Contains(lastMouseMapHitPos))
						{
							candidateCountryIndex = c;
							candidateCountryRegionIndex = cr;
							if (_showProvinces || _enableProvinceHighlight)
								if (country.provinces != null)
									for (var p = 0; p < country.provinces.Length; p++)
									{
										// and now, we check if the mouse if inside a province
										var province = country.provinces[p];
										if (province.regions ==
										    null) // read province data the first time we need it
											ReadProvincePackedString(province);
										if (province.regionsRect2D.Contains(lastMouseMapHitPos))
										{
											var regCount = province.regions.Count;
											for (var pr = 0; pr < regCount; pr++)
											{
												var provRegion = province.regions[pr];
												if (provRegion.rect2DArea < candidateProvinceRegionSize &&
												    provRegion.Contains(lastMouseMapHitPos))
												{
													candidateProvinceRegionSize = provRegion.rect2DArea;
													candidateProvinceIndex = GetProvinceIndex(province);
													candidateProvinceRegionIndex = pr;
												}
											}
										}
									}
							break;
						}
					}
					if (candidateCountryIndex >= 0)
						break;
				}
				// If no candidate country found, try looking into provinces directly just in case some province doesn't have a country region on the same area
				if (candidateCountryIndex < 0 && (_showProvinces || _enableProvinceHighlight))
				{
					var provincesCount = provinces.Length;
					for (var p = 0; p < provincesCount; p++)
					{
						var province = _provinces[p];
						if (province.regionsRect2D.Contains(lastMouseMapHitPos))
						{
							if (province.regions == null)
								ReadProvincePackedString(province);
							var regionsCount = province.regions.Count;
							for (var pr = 0; pr < regionsCount; pr++)
							{
								var provRegion = province.regions[pr];
								if (provRegion.rect2DArea < candidateProvinceRegionSize &&
								    provRegion.Contains(lastMouseMapHitPos))
								{
									candidateProvinceRegionSize = provRegion.rect2DArea;
									candidateProvinceIndex = p;
									candidateProvinceRegionIndex = pr;
									candidateCountryIndex = province.countryIndex;
									candidateCountryRegionIndex =
										countries[candidateCountryIndex]
											.mainRegionIndex; // fallback; we don't have a specific country region under this province so we link it to the main region
								}
							}
						}
					}
				}
			}
			else
			{
				candidateCountryIndex = _countryHighlightedIndex;
				candidateCountryRegionIndex = _countryRegionHighlightedIndex;
				candidateProvinceIndex = _provinceHighlightedIndex;
				candidateProvinceRegionIndex = _provinceRegionHighlightedIndex;
			}

			if (candidateCountryIndex != _countryHighlightedIndex ||
			    candidateCountryIndex == _countryHighlightedIndex &&
			    candidateCountryRegionIndex != _countryRegionHighlightedIndex)
			{
				if (candidateCountryIndex < 0)
					HideCountryRegionHighlight();
				else
				{
					// Raise enter event
					if (candidateCountryIndex >= 0 && candidateCountryRegionIndex >= 0)
					{
						if (OnCountryEnter != null)
							OnCountryEnter(candidateCountryIndex, candidateCountryRegionIndex);
						if (OnRegionEnter != null)
							OnRegionEnter(_countries[candidateCountryIndex]
								.regions[candidateCountryRegionIndex]);
					}

					HighlightCountryRegion(candidateCountryIndex, candidateCountryRegionIndex, false,
						_showOutline);

					if (_showProvinces)
						DrawProvinces(candidateCountryIndex, false, false,
							false); // draw provinces borders if not drawn

					// Draw province labels?
					if (_showProvinceNames &&
					    _provinceLabelsVisibility == PROVINCE_LABELS_VISIBILITY.Automatic &&
					    _showAllCountryProvinceNames)
						RedrawProvinceLabels(_countries[candidateCountryIndex]);
				}
			}

			if (candidateProvinceIndex != _provinceHighlightedIndex ||
			    candidateProvinceIndex == _provinceHighlightedIndex &&
			    candidateProvinceRegionIndex != _provinceRegionHighlightedIndex)
			{
				// Raise enter event
				if (candidateProvinceIndex >= 0 && candidateProvinceRegionIndex >= 0)
				{
					if (OnProvinceEnter != null)
						OnProvinceEnter(candidateProvinceIndex, candidateProvinceRegionIndex);

					if (OnRegionEnter != null)
						OnRegionEnter(_provinces[candidateProvinceIndex]
							.regions[candidateProvinceRegionIndex]);
				}

				HighlightProvinceRegion(candidateProvinceIndex, candidateProvinceRegionIndex, false);

				// Draw province labels?
				if (_showProvinceNames &&
				    _provinceLabelsVisibility == PROVINCE_LABELS_VISIBILITY.Automatic &&
				    !_showAllCountryProvinceNames)
					RedrawProvinceLabels(_countries[candidateCountryIndex]);
			}

			// Verify if a city is hit inside selected country
			if (_showCities)
				CheckMousePosCity(lastMouseMapHitPos);
		}

		private void CheckMousePosCity(Vector3 localPoint)
		{
			var ci = GetCityNearPoint(localPoint, _countryHighlightedIndex);
			if (ci >= 0)
			{
				if (ci != _cityHighlightedIndex)
				{
					HideCityHighlight();
					HighlightCity(ci);
				}
			}
			else if (_cityHighlightedIndex >= 0)
				HideCityHighlight();
		}

		#endregion

		#region Internal API

		private float ApplyDragThreshold(float value)
		{
			if (_mouseDragThreshold > 0)
			{
				if (value < 0)
				{
					value += _mouseDragThreshold;
					if (value > 0)
						value = 0;
				}
				else
				{
					value -= _mouseDragThreshold;
					if (value < 0)
						value = 0;
				}
			}
			return value;
		}

		/// <summary>
		/// Returns the overlay base layer (parent gameObject), useful to overlay stuff on the map (like labels). It will be created if it doesn't exist.
		/// </summary>
		public GameObject GetOverlayLayer(bool createIfNotExists)
		{
			if (overlayLayer != null)
				return overlayLayer;
			if (createIfNotExists)
				return CreateOverlay();
			return null;
		}

		private void SetDestination(Vector2 point, float duration)
		{
			SetDestination(point, duration, GetZoomLevel());
		}

		private void SetDestination(Vector2 point, float duration, float zoomLevel)
		{
			var distance = GetZoomLevelDistance(zoomLevel);
			SetDestinationAndDistance(point, duration, distance);
		}

		private void SetDestinationAndDistance(Vector2 point, float duration, float distance)
		{
			// if map is in world-wrapping mode, offset the point to the appropriate side of the map
			if (_wrapHorizontally)
			{
				var x = _cursorLocation.x;
				Vector3 localPos;
				if (GetCurrentMapLocation(out localPos))
					x = localPos.x;
				var rightSide = point.x + 1f;
				var leftSide = point.x - 1f;
				var distNormal = Mathf.Abs(point.x - x);
				var distRightSide = Mathf.Abs(rightSide - x);
				var distLeftSide = Mathf.Abs(leftSide - x);
				if (distRightSide < distNormal)
					point.x = rightSide;
				else if (distLeftSide < distNormal)
					point.x = leftSide;
			}

			// save params call (used by RecalculateFlyToParams)
			flyToCallParamsPoint = point;
			flyToCallZoomDistance = distance;

			// setup lerping parameters
			var cam = currentCamera;
			if (cam == null)
				return;
			flyToStartQuaternion = cam.transform.rotation;
			flyToStartLocation = cam.transform.position;
			if (_enableFreeCamera)
			{
				flyToEndQuaternion = flyToStartQuaternion;
				flyToEndLocation = transform.TransformPoint(point) - cam.transform.forward * distance;
			}
			else
			{
				flyToEndQuaternion = transform.rotation;
				var offset =
					cam.ViewportToWorldPoint(new Vector3(_flyToScreenCenter.x, _flyToScreenCenter.y,
						distance)) -
					cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, distance));
				flyToEndLocation = transform.TransformPoint(point) - transform.forward * distance - offset;
			}
			flyToDuration = duration;
			flyToActive = true;
			flyToStartTime = Time.time;
			if (OnFlyStart != null)
				OnFlyStart();
			if (flyToDuration == 0)
				MoveCameraToDestination();
		}

		/// <summary>
		/// Returns the distance from camera to map according to fit to window width/height parameters
		/// </summary>
		/// <returns>The frustum distance.</returns>
		private float GetFrustumDistance()
		{
			var cam = currentCamera;
			if (cam == null)
				return 1;

			var fv = cam.fieldOfView;
			var aspect = cam.aspect;
			var radAngle = fv * Mathf.Deg2Rad;
			float distance, frustumDistanceW, frustumDistanceH;
			if (currentCamera.orthographic)
			{
				if (_fitWindowHeight)
				{
					cam.orthographicSize = mapHeight * 0.5f * _windowRect.height;
					maxFrustumDistance = cam.orthographicSize;
				}
				else if (_fitWindowWidth)
				{
					cam.orthographicSize = mapWidth * 0.5f * _windowRect.width / aspect;
					maxFrustumDistance = cam.orthographicSize;
				}
				else
					maxFrustumDistance = float.MaxValue;
				distance = 1;
			}
			else
			{
				frustumDistanceH = mapHeight * _windowRect.height * 0.5f / Mathf.Tan(radAngle * 0.5f);
				frustumDistanceW =
					mapWidth * _windowRect.width / aspect * 0.5f / Mathf.Tan(radAngle * 0.5f);
				if (_fitWindowHeight)
				{
					distance = Mathf.Min(frustumDistanceH, frustumDistanceW);
					maxFrustumDistance = distance;
				}
				else if (_fitWindowWidth)
				{
					distance = Mathf.Max(frustumDistanceH, frustumDistanceW);
					maxFrustumDistance = distance;
				}
				else
				{
					var plane = new Plane(-transform.forward, transform.position);
					distance = plane.GetDistanceToPoint(cam.transform.position);
					maxFrustumDistance = float.MaxValue;
				}
			}
			return distance;
		}

		private float GetCameraDistance()
		{
			var plane = new Plane(-transform.forward, transform.position);
			return plane.GetDistanceToPoint(currentCamera.transform.position);
		}

		/// <summary>
		/// Returns optimum distance between camera and a region of width/height
		/// </summary>
		private float GetFrustumZoomLevel(float width, float height)
		{
			if (currentCamera == null)
				return 1;
			var fv = _currentCamera.fieldOfView;
			var aspect = _currentCamera.aspect;
			var radAngle = fv * Mathf.Deg2Rad;
			float distance, frustumDistanceW, frustumDistanceH;
			if (_currentCamera.orthographic)
				distance = 1;
			else
			{
				frustumDistanceH = height * 0.5f / Mathf.Tan(radAngle * 0.5f);
				frustumDistanceW = width / aspect * 0.5f / Mathf.Tan(radAngle * 0.5f);
				distance = Mathf.Max(frustumDistanceH, frustumDistanceW);
			}
			var referenceDistance = GetZoomLevelDistance(1f);
			return distance / referenceDistance;
		}

		/// <summary>
		/// Gets the distance according to the zoomLevel. The zoom level is a value between 0 and 1 which maps to 0-max zoom distance parameter.
		/// </summary>
		/// <returns>The zoom level distance.</returns>
		private float GetZoomLevelDistance(float zoomLevel)
		{
			var cam = currentCamera;
			if (cam == null)
				return 0;

			zoomLevel = Mathf.Clamp01(zoomLevel);

			var fv = cam.fieldOfView;
			var radAngle = fv * Mathf.Deg2Rad;
			var aspect = cam.aspect;
			var frustumDistanceH = mapHeight * 0.5f / Mathf.Tan(radAngle * 0.5f);
			var frustumDistanceW = mapWidth / aspect * 0.5f / Mathf.Tan(radAngle * 0.5f);
			float distance;
			if (_fitWindowWidth)
				distance = Mathf.Max(frustumDistanceH, frustumDistanceW);
			else
				distance = Mathf.Min(frustumDistanceH, frustumDistanceW);
			return distance * zoomLevel;
		}

		/// <summary>
		/// Returns the current distance to map from the camera
		/// </summary>
		public float GetMapDistance()
		{
			var cam = currentCamera;
			if (cam == null)
				return 0;

			var p = new Plane(transform.forward, transform.position);
			return p.GetDistanceToPoint(cam.transform.position);
		}

		/// <summary>
		/// Used internally to translate the camera during FlyTo operations. Use FlyTo method.
		/// </summary>
		private void MoveCameraToDestination()
		{
			float delta;
			Quaternion rotation;
			Vector3 destination;
			if (flyToDuration == 0)
			{
				delta = 0;
				rotation = flyToEndQuaternion;
				destination = flyToEndLocation;
			}
			else
			{
				delta = Time.time - flyToStartTime;
				var t = delta / flyToDuration;
				var st = Mathf.SmoothStep(0, 1, t);
				rotation = Quaternion.Lerp(flyToStartQuaternion, flyToEndQuaternion, st);
				destination = Vector3.Lerp(flyToStartLocation, flyToEndLocation, st);
			}
			_currentCamera.transform.rotation = rotation;
			_currentCamera.transform.position = destination;

			if (delta >= flyToDuration)
			{
				flyToActive = false;
				if (OnFlyEnd != null)
					OnFlyEnd();
			}
		}

		// Updates flyTo params due to a change in viewport mode
		private void RepositionCamera()
		{
			if (renderViewPortIsTerrain)
			{
				var terrainCenter = terrain.GetPosition();
				terrainCenter.x += terrain.terrainData.size.x * 0.5f;
				terrainCenter.y += 250f;
				terrainCenter.z += terrain.terrainData.size.z * 0.5f;
				cameraMain.transform.position = terrainCenter;
				cameraMain.transform.forward = Vector3.down;
			}

			// When changing viewport mode, the asset changes cameras, so we take the current location and zoom level and updates the new frustrum distance and other things calling CenterMap()
			// then we move the new camera to that location and zoom level and optionally update flyTo params.
			CenterMap();

			// Changes new camera to current map position
			_currentCamera.transform.rotation = transform.rotation;
			_currentCamera.transform.position = transform.TransformPoint(lastKnownMapCoordinates) -
			                                    transform.forward *
			                                    GetZoomLevelDistance(lastKnownZoomLevel);

			// update lerping parameters based on the new camera setup and original destination and zoom level
			if (!flyToActive)
				return;
			flyToStartQuaternion = _currentCamera.transform.rotation;
			flyToStartLocation = _currentCamera.transform.position;
			flyToEndQuaternion = transform.rotation;
			flyToEndLocation = transform.TransformPoint(flyToCallParamsPoint) -
			                   transform.forward * flyToCallZoomDistance;
			MoveCameraToDestination();
		}

		private Material GetColoredTexturedMaterial(Color color, Texture2D texture) =>
			GetColoredTexturedMaterial(color, texture, true);

		private Material GetColoredTexturedMaterial(Color color, Texture2D texture,
			bool autoChooseTransparentMaterial, int renderQueueIncrement = 0)
		{
			Material customMat;
			if (texture == null)
				if (cacheMaterials && coloredMatCache.TryGetValue(color, out customMat))
				{
					customMat.renderQueue += renderQueueIncrement;
					return customMat;
				}
			if (texture != null)
			{
				customMat = Instantiate(texturizedMat);
				customMat.name = texturizedMat.name;
				customMat.mainTexture = texture;
			}
			else
			{
				if (color.a < 1.0f || !autoChooseTransparentMaterial)
					customMat = Instantiate(coloredAlphaMat);
				else
					customMat = Instantiate(coloredMat);
				customMat.name = coloredMat.name;
				if (cacheMaterials)
					coloredMatCache[color] = customMat;
			}
			customMat.color = color;
			if (disposalManager != null)
				disposalManager.MarkForDisposal(customMat);
			customMat.renderQueue += renderQueueIncrement;
			return customMat;
		}

		private Material GetColoredMarkerMaterial(Color color)
		{
			Material customMat;
			if (markerMatCache.TryGetValue(color, out customMat))
				return customMat;
			customMat = Instantiate(markerMat);
			customMat.name = markerMat.name;
			markerMatCache[color] = customMat;
			customMat.color = color;
			if (disposalManager != null)
				disposalManager.MarkForDisposal(customMat); //.hideFlags = HideFlags.DontSave;
			return customMat;
		}

		private void ApplyMaterialToSurface(GameObject obj, Material sharedMaterial)
		{
			if (obj != null)
			{
				var rr = obj
					.GetComponentsInChildren<Renderer>(
						true); // surfaces can be saved under parent when Include All Regions is enabled
				for (var k = 0; k < rr.Length; k++)
					rr[k].sharedMaterial = sharedMaterial;
			}
		}

		private void GetPointFromPackedString(ref string s, out float x, out float y)
		{
			//			int j = -1;
			//			for (int k = 0; k < s.Length; k++) {
			//				if (s [k] == ',') {
			//					j = k;
			//					break;
			//				}
			//			}
			//			if (j < 0) {
			//				x = 0;
			//				y = 0;
			//				return;
			//			}
			//			string sx = s.Substring (0, j);
			//			float.TryParse (sx, NumberStyles.Float, Misc.InvariantCulture, out x);
			//			x /= MAP_PRECISION;
			//			string sy = s.Substring (j + 1);
			//			float.TryParse (sy, NumberStyles.Float, Misc.InvariantCulture, out y);
			//			y /= MAP_PRECISION;

			var d = 1;
			float v = 0;
			y = 0;
			for (var k = s.Length - 1; k >= 0; k--)
			{
				var ch = s[k];
				if (ch >= '0' && ch <= '9')
				{
					v += (ch - '0') * d;
					d *= 10;
				}
				else if (ch == '.')
				{
					v = v / d;
					d = 1;
				}
				else if (ch == '-')
					v = -v;
				else if (ch == ',')
				{
					y = v / MAP_PRECISION;
					v = 0;
					d = 1;
				}
			}
			x = v / MAP_PRECISION;
		}

		/// <summary>
		/// Internal usage.
		/// </summary>
		public int GetUniqueId(List<IExtendableAttribute> list)
		{
			for (var k = 0; k < 1000; k++)
			{
				var rnd = Random.Range(0, int.MaxValue);
				var listCount = list.Count;
				for (var o = 0; o < listCount; o++)
				{
					var obj = list[o];
					if (obj != null && obj.uniqueId == rnd)
					{
						rnd = 0;
						break;
					}
				}
				if (rnd > 0)
					return rnd;
			}
			return 0;
		}

		/// <summary>
		/// Internal usage. Checks quality of polygon points. Useful before using polygon clipping operations.
		/// Return true if there're changes.
		/// </summary>
		public bool RegionSanitize(Region region)
		{
			var changes = false;
			var points = region.points;
			// removes points which are too near from others
			for (var k = 0; k < points.Length; k++)
			{
				var x0 = points[k].x;
				var y0 = points[k].y;
				for (var j = k + 1; j < points.Length; j++)
				{
					var x1 = points[j].x;
					var y1 = points[j].y;
					var distance = (x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0);
					if (distance < 0.00000000001f)
					{
						points = points.Purge(j);
						j--;
						changes = true;
					}
				}
			}
			// remove crossing segments
			if (PolygonSanitizer.RemoveCrossingSegments(ref points))
				changes = true;
			if (changes)
				region.points = points;
			region.sanitized = true;
			return changes;
		}

		/// <summary>
		/// Checks for the sanitized flag in regions list and invoke RegionSanitize on pending regions
		/// </summary>
		private void RegionSanitize(List<Region> regions)
		{
			regions.ForEach(region =>
			{
				if (!region.sanitized)
					RegionSanitize(region);
			});
		}

		/// <summary>
		/// Makes a region collapse with the neigbhours frontiers - needed when merging two adjacent regions
		/// </summary>
		private void RegionMagnet(Region region, Region neighbourRegion)
		{
			const float tolerance = 1e-6f;
			var pointCount = region.points.Length;
			var usedPoints = new bool[pointCount];
			var otherPointCount = neighbourRegion.points.Length;
			var usedOtherPoints = new bool[otherPointCount];

			for (var i = 0; i < pointCount; i++)
			{
				var minDist = float.MaxValue;
				var selPoint = -1;
				var selOtherPoint = -1;
				for (var p = 0; p < pointCount; p++)
				{
					if (usedPoints[p])
						continue;
					var point0 = region.points[p];
					for (var o = 0; o < otherPointCount; o++)
					{
						if (usedOtherPoints[o])
							continue;
						var point1 = neighbourRegion.points[o];
						var dx = point0.x - point1.x;
						if (dx < 0)
							dx = -dx;
						if (dx < tolerance)
						{
							var dy = point0.y - point1.y;
							if (dy < 0)
								dy = -dy;
							if (dy < tolerance)
							{
								var dist = dx < dy ? dx : dy;
								if (dist <= 0)
								{
									usedPoints[p] = true;
									usedOtherPoints[o] = true;
									break;
								}
								if (dist < minDist)
								{
									minDist = dist;
									selPoint = p;
									selOtherPoint = o;
								}
							}
						}
					}
				}
				if (selPoint >= 0)
				{
					region.points[selPoint] = neighbourRegion.points[selOtherPoint];
					region.sanitized = false;
					usedPoints[selPoint] = true;
					usedOtherPoints[selOtherPoint] = true;
				}
				else
					break;
			}
		}

		/// <summary>
		/// Removes special characters from string.
		/// </summary>
		private string DataEscape(string s)
		{
			s = s.Replace("$", "");
			s = s.Replace("|", "");
			return s;
		}

		#endregion

		#region World Gizmos

		private void CheckCursorVisibility()
		{
			if (_showCursor)
			{
				if (cursorLayerHLine != null)
				{
					var visible = cursorLayerHLine.activeSelf;
					if (_showTiles)
					{
						if (_currentZoomLevel > TILE_MAX_CURSOR_ZOOM_LEVEL && cursorLayerHLine.activeSelf)
							visible = false;
						else if (_currentZoomLevel <= TILE_MAX_CURSOR_ZOOM_LEVEL &&
						         !cursorLayerHLine.activeSelf)
							visible = true;
					}
					if ((mouseIsOverUIElement || !mouseIsOver) &&
					    visible &&
					    !cursorAlwaysVisible) // not over map?
						visible = false;
					else if (!mouseIsOverUIElement &&
					         mouseIsOver &&
					         !visible) // finally, should be visible?
						visible = true;
					if (cursorLayerHLine.activeSelf != visible)
						cursorLayerHLine.SetActive(visible);
				}
				if (cursorLayerVLine != null)
				{
					var visible = cursorLayerVLine.activeSelf;
					if (_showTiles)
					{
						if (_currentZoomLevel > TILE_MAX_CURSOR_ZOOM_LEVEL && cursorLayerVLine.activeSelf)
							visible = false;
						else if (_currentZoomLevel <= TILE_MAX_CURSOR_ZOOM_LEVEL &&
						         !cursorLayerVLine.activeSelf)
							visible = true;
					}
					if ((mouseIsOverUIElement || !mouseIsOver) &&
					    visible &&
					    !cursorAlwaysVisible) // not over map?
						visible = false;
					else if (!mouseIsOverUIElement &&
					         mouseIsOver &&
					         !visible) // finally, should be visible?
						visible = true;
					if (cursorLayerVLine.activeSelf != visible)
						cursorLayerVLine.SetActive(visible);
				}
			}
		}

		private void DrawCursor()
		{
			if (!_showCursor)
				return;

			// Generate line V **********************
			var points = new Vector3[2];
			var indices = new int[2];
			indices[0] = 0;
			indices[1] = 1;
			points[0] = Misc.Vector3up * -0.5f;
			points[1] = Misc.Vector3up * 0.5f;

			var t = transform.Find("CursorV");
			if (t != null)
				DestroyImmediate(t.gameObject);
			cursorLayerVLine = new GameObject("CursorV");
			cursorLayerVLine.transform.SetParent(transform, false);
			cursorLayerVLine.transform.localPosition = Misc.Vector3back * 0.00001f; // needed for minimap
			cursorLayerVLine.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
			cursorLayerVLine.layer = gameObject.layer;
			cursorLayerVLine.SetActive(_showCursor);

			var meshH = new Mesh();
			meshH.vertices = points;
			meshH.SetIndices(indices, MeshTopology.Lines, 0);
			meshH.RecalculateBounds();

			var mf = cursorLayerVLine.AddComponent<MeshFilter>();
			mf.sharedMesh = meshH;

			var mr = cursorLayerVLine.AddComponent<MeshRenderer>();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
			mr.shadowCastingMode = ShadowCastingMode.Off;
			mr.sharedMaterial = cursorMatV;

			// Generate line H **********************
			points[0] = Misc.Vector3right * -0.5f;
			points[1] = Misc.Vector3right * 0.5f;

			t = transform.Find("CursorH");
			if (t != null)
				DestroyImmediate(t.gameObject);
			cursorLayerHLine = new GameObject("CursorH");
			cursorLayerHLine.transform.SetParent(transform, false);
			cursorLayerHLine.transform.localPosition = Misc.Vector3back * 0.00001f; // needed for minimap
			cursorLayerHLine.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
			cursorLayerHLine.layer = gameObject.layer;
			cursorLayerHLine.SetActive(_showCursor);

			var meshV = new Mesh();
			meshV.vertices = points;
			meshV.SetIndices(indices, MeshTopology.Lines, 0);
			meshV.RecalculateBounds();

			mf = cursorLayerHLine.AddComponent<MeshFilter>();
			mf.sharedMesh = meshV;

			mr = cursorLayerHLine.AddComponent<MeshRenderer>();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
			mr.shadowCastingMode = ShadowCastingMode.Off;
			mr.sharedMaterial = cursorMatH;
		}

		private void DrawImaginaryLines()
		{
			DrawLatitudeLines();
			DrawLongitudeLines();
		}

		private void DrawLatitudeLines()
		{
			if (!_showLatitudeLines)
				return;

			// Generate latitude lines
			var points = new List<Vector3>();
			var indices = new List<int>();
			var r = 0.5f;
			var idx = -1;

			for (float a = 0; a < 90; a += _latitudeStepping)
			{
				for (var h = 1; h >= -1; h--)
				{
					if (h == 0)
						continue;
					var y = h * a / 90.0f * r;
					points.Add(new Vector3(-r, y, 0));
					points.Add(new Vector3(r, y, 0));
					indices.Add(++idx);
					indices.Add(++idx);
					if (a == 0)
						break;
				}
			}

			var t = transform.Find("LatitudeLines");
			if (t != null)
				DestroyImmediate(t.gameObject);
			latitudeLayer = new GameObject("LatitudeLines");
			if (disposalManager != null)
				disposalManager.MarkForDisposal(latitudeLayer); //.hideFlags = HideFlags.DontSave;
			latitudeLayer.transform.SetParent(transform, false);
			latitudeLayer.transform.localPosition = Misc.Vector3zero;
			latitudeLayer.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
			latitudeLayer.layer = gameObject.layer;
			latitudeLayer.SetActive(_showLatitudeLines);

			var mesh = new Mesh();
			mesh.vertices = points.ToArray();
			mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
			mesh.RecalculateBounds();
			if (disposalManager != null)
				disposalManager.MarkForDisposal(mesh); //.hideFlags = HideFlags.DontSave;

			var mf = latitudeLayer.AddComponent<MeshFilter>();
			mf.sharedMesh = mesh;

			var mr = latitudeLayer.AddComponent<MeshRenderer>();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
			mr.shadowCastingMode = ShadowCastingMode.Off;
			//			mr.useLightProbes = false;
			mr.sharedMaterial = imaginaryLinesMat;
		}

		private void DrawLongitudeLines()
		{
			if (!_showLongitudeLines)
				return;

			// Generate longitude lines
			var points = new List<Vector3>();
			var indices = new List<int>();
			var r = 0.5f;
			var idx = -1;
			var step = 180 / _longitudeStepping;

			for (float a = 0; a < 90; a += step)
			{
				for (var h = 1; h >= -1; h--)
				{
					if (h == 0)
						continue;
					var x = h * a / 90.0f * r;
					points.Add(new Vector3(x, -r, 0));
					points.Add(new Vector3(x, r, 0));
					indices.Add(++idx);
					indices.Add(++idx);
					if (a == 0)
						break;
				}
			}

			var t = transform.Find("LongitudeLines");
			if (t != null)
				DestroyImmediate(t.gameObject);
			longitudeLayer = new GameObject("LongitudeLines");
			if (disposalManager != null)
				disposalManager.MarkForDisposal(longitudeLayer); //.hideFlags = HideFlags.DontSave;
			longitudeLayer.transform.SetParent(transform, false);
			longitudeLayer.transform.localPosition = Misc.Vector3zero;
			longitudeLayer.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
			longitudeLayer.layer = gameObject.layer;
			longitudeLayer.SetActive(_showLongitudeLines);

			var mesh = new Mesh();
			mesh.vertices = points.ToArray();
			mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
			mesh.RecalculateBounds();
			if (disposalManager != null)
				disposalManager.MarkForDisposal(mesh); //.hideFlags = HideFlags.DontSave;

			var mf = longitudeLayer.AddComponent<MeshFilter>();
			mf.sharedMesh = mesh;

			var mr = longitudeLayer.AddComponent<MeshRenderer>();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
			mr.shadowCastingMode = ShadowCastingMode.Off;
			mr.sharedMaterial = imaginaryLinesMat;
		}

		#endregion

		#region Overlay

		public GameObject CreateOverlay()
		{
			// 2D labels layer
			var t = transform.Find(OVERLAY_BASE);
			if (t == null)
			{
				overlayLayer = new GameObject(OVERLAY_BASE);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(overlayLayer); //.hideFlags = HideFlags.DontSave;
				overlayLayer.transform.SetParent(transform, false);
				overlayLayer.transform.localPosition = Misc.Vector3back * 0.002f;
				overlayLayer.transform.localScale = Misc.Vector3one;
				overlayLayer.layer = gameObject.layer;
			}
			else
			{
				overlayLayer = t.gameObject;
				overlayLayer.SetActive(true);
			}
			return overlayLayer;
		}

		private void UpdateScenicPlusDistance()
		{
			if (earthMat == null)
				return;
			var zoomLevel = GetZoomLevel();
			earthMat.SetFloat("_Distance", zoomLevel);
		}

		#endregion

		#region Markers support

		private void CheckMarkersLayer()
		{
			if (markersLayer == null)
			{
				// try to capture an existing marker layer
				var t = transform.Find("Markers");
				if (t != null)
					markersLayer = t.gameObject;
			}
			if (markersLayer == null)
			{
				// create it otherwise
				markersLayer = new GameObject("Markers");
				markersLayer.transform.SetParent(transform, false);
				markersLayer.layer = transform.gameObject.layer;
			}
		}

		#endregion

		#region Global Events handling

		internal void BubbleEvent<T>(Action<T> a, T o)
		{
			if (a != null)
				a(o);
		}

		#endregion
	}
}