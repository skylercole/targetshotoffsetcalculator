using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraPinchZoom : MonoBehaviour
{
    public Canvas canvas;
    public GameObject bulletPrefab;
    public GameObject undo;
    public GameObject menu;
    public GameObject redDot;
    public GameObject greenDot;
    public GameObject center;
    public GameObject airgunCenter;
    public GameObject menuPanel;
    public GameObject textPortrait;
    public GameObject textLandscape;
    public Button undoButton;
    public Button menuButton;
    public Button setArcheryButton;
    public Button setAirgunButton;
    public AudioSource increaseAudio;
    public AudioSource decreaseAudio;

    private List<GameObject> bullets;
    
    public Camera theCamera;
    public float zoomSpeed;
    float previousDistance;
    float panSpeed = -0.03f;
    float origZoom;
    Vector3 camPos;

    private float holdTime = 0.5f;
    private float acumTime;

    Vector3 originalMenuScale;

    float timeAfterZoom;
    readonly float timeBeforeMove = 0.3f;
    readonly float maxZoom = 1.2f;
    readonly float minZoom = 5.6f;

    public BoxCollider colliderMenu, colliderUndo;

    void Start()
    {        
        origZoom = theCamera.orthographicSize;
        camPos = theCamera.transform.position;
        acumTime = 0;
        bullets = new List<GameObject>();
        timeAfterZoom = 0;
        
        undoButton.onClick.AddListener(UndoLast);
        menuButton.onClick.AddListener(MenuPressed);
        setArcheryButton.onClick.AddListener(SetArchery);
        setAirgunButton.onClick.AddListener(SetAirgun);

        airgunCenter.SetActive(false);
        redDot.SetActive(false);
        greenDot.SetActive(false);
        menuPanel.SetActive(false);
        OrientationChange();

        InvokeRepeating("CalculateAverage", 0, 0.2f);

        textLandscape.SetActive(false);
        textPortrait.SetActive(true);
    }

    void SetArchery()
    {
        airgunCenter.SetActive(false);
        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    void SetAirgun()
    {
        airgunCenter.SetActive(true);
        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    void MenuPressed()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);        
        undo.SetActive(!menuPanel.activeSelf);
    }

    void UndoLast()
    {
        //Debug.Log("Archery: UndoLast clicked");

        if (bullets.Count > 0)
        {
            decreaseAudio.Play(0);
            var bullet = bullets[bullets.Count - 1];
            Destroy(bullet);
            bullets.RemoveAt(bullets.Count - 1);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuPanel.activeSelf)
            {
                MenuPressed();
            }
            else
            { 
                Application.Quit();
            }
        }

        OrientationChange();

        if (menuPanel.activeSelf)
        {
            return;
        }

        if (bullets.Count == 0)
        {
            undo.SetActive(false);
        }
        else
        {
            undo.SetActive(true);
        }

        timeAfterZoom -= Time.deltaTime;

        // On double tap image will be set at original position and scale
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).tapCount == 2)
        {
            theCamera.orthographicSize = origZoom;
            theCamera.transform.position = camPos;
        }
        // Single tap and hold
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Stationary)
        {
            acumTime += Time.deltaTime;

            if (acumTime >= holdTime)
            {
                // Long tap registered
                var touch = Input.GetTouch(0).position;                
                var locationVec = new Vector3(touch.x, touch.y, 100f);

                // If bullet has been created already in a previous frame.
                if (bullets.Find(x => x.transform.position == theCamera.ScreenToWorldPoint(locationVec)))
                {
                    //Debug.Log("dbg found same location already filled");
                    acumTime = 0;
                    return;
                }

                increaseAudio.Play(0);

                var b = Instantiate(bulletPrefab, canvas.transform);
                
                b.transform.position = theCamera.ScreenToWorldPoint(locationVec);

                bullets.Add(b);

                //Debug.Log("dbg bullets count: " + bullets.Count);

                acumTime = 0;
            }
        }

        // Move around
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved && timeAfterZoom <= 0)
        {
            // Works better than Input.GetTouch for one finger moves.
            float x = Input.GetAxis("Mouse X") * panSpeed;
            float y = Input.GetAxis("Mouse Y") * panSpeed;

            if (!colliderMenu.bounds.Contains(new Vector3(x, y, menu.transform.position.z)) && !colliderUndo.bounds.Contains(new Vector3(x, y, undo.transform.position.z)))
            {
                transform.Translate(x, y, 0);
            }            
        }
        // Pinch and zoom phase 1
        else if (Input.touchCount == 2 && (Input.GetTouch(0).phase == TouchPhase.Began || Input.GetTouch(1).phase == TouchPhase.Began))
        {
            Vector2 touch1 = Input.GetTouch(0).position;
            Vector2 touch2 = Input.GetTouch(1).position;
            //get the initial distance
            previousDistance = Vector2.Distance(touch1, touch2);            
        }
        // Pinch and zoom phase 2
        else if (Input.touchCount == 2 && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved))
        {
            float distance;

            Vector2 touch1 = Input.GetTouch(0).position;
            Vector2 touch2 = Input.GetTouch(1).position;

            distance = Vector2.Distance(touch1, touch2);

            float pinchAmount = (previousDistance - distance) * zoomSpeed * Time.deltaTime;
            
            Vector2 midPoint = (touch1 + touch2) / 2f;

            float multiplier = (1.0f / theCamera.orthographicSize * pinchAmount);
            Vector3 zoomTowards = new Vector3(midPoint.x, midPoint.y, 0);
            zoomTowards = theCamera.ScreenToWorldPoint(zoomTowards);            

            if (theCamera.orthographicSize + pinchAmount < origZoom * maxZoom && theCamera.orthographicSize + pinchAmount > origZoom / minZoom)
            {
                // Zoom camera
                theCamera.orthographicSize += pinchAmount;

                float z = theCamera.transform.position.z;

                // Move camera
                theCamera.transform.position -= (zoomTowards - transform.position) * multiplier;
                theCamera.transform.position = new Vector3(theCamera.transform.position.x, theCamera.transform.position.y, z);
            }

            previousDistance = distance;

            timeAfterZoom = timeBeforeMove;
        }
                
        // Reset single tap wait time
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            acumTime = 0;
        }
    }

    void CalculateAverage()
    {
        if (bullets.Count > 1)
        {
            Vector3 avg = Vector3.zero;

            foreach (var bullet in bullets)
            {
                avg += bullet.transform.position;
            }
                        
            redDot.transform.position = new Vector3((avg / bullets.Count).x, (avg / bullets.Count).y, redDot.transform.position.z);
            
            var greenX = 2 * center.transform.position.x - redDot.transform.position.x;
            var greenY = 2 * center.transform.position.y - redDot.transform.position.y;
            greenDot.transform.position = new Vector3(greenX, greenY, greenDot.transform.position.z);

            redDot.SetActive(true);
            greenDot.SetActive(true);
        }
        else
        {
            redDot.SetActive(false);
            greenDot.SetActive(false);
        }
    }

    void OrientationChange()
    {
        if (!textLandscape.activeSelf && (Input.deviceOrientation == DeviceOrientation.LandscapeLeft || Input.deviceOrientation == DeviceOrientation.LandscapeRight))
        {
            textLandscape.SetActive(true);
            textPortrait.SetActive(false);
        }
        else if(!textPortrait.activeSelf && Input.deviceOrientation == DeviceOrientation.Portrait)
        {
            textLandscape.SetActive(false);
            textPortrait.SetActive(true);
        }
    }
}