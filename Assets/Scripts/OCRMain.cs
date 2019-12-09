﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OCRMain : MonoBehaviour {

    // CAMERA
    private bool camAvailable;
    private WebCamTexture backCam;
    private Texture defaultBackground;

    public RawImage background;
    public AspectRatioFitter fit;

    //OCR
    private string url = "https://vision.googleapis.com/v1/images:annotate?key=";
    public string apiKey = "";
    private FeatureType featureType = FeatureType.TEXT_DETECTION;
    public int maxResults = 10;

    Texture2D texture2D;
    Dictionary<string, string> headers;
    List<string> dishes;
    public static List<string> matchedDishes;

    public Button myButton;
    // public Text myText;

    [System.Serializable]
    public class AnnotateImageRequests {
        public List<AnnotateImageRequest> requests;
    }

    [System.Serializable]
    public class AnnotateImageRequest {
        public Image image;
        public List<Feature> features;
    }

    [System.Serializable]
    public class Image {
        public string content;
    }

    [System.Serializable]
    public class Feature {
        public string type;
        public int maxResults;
    }

    [System.Serializable]
    public class ImageContext {
        // public LatLongRect latLongRect;
        public List<string> languageHints;
    }

    // [System.Serializable]
    // public class LatLongRect {
    //     public LatLng minLatLng;
    //     public LatLng maxLatLng;
    // }

    [System.Serializable]
    public class AnnotateImageResponses {
        public List<AnnotateImageResponse> responses;
    }

    [System.Serializable]
    public class AnnotateImageResponse {
        public List<EntityAnnotation> textAnnotations;
    }

    [System.Serializable]
    public class EntityAnnotation {
        public string description;
    }

    // [System.Serializable]
    // public class LatLng {
    //     float latitude;
    //     float longitude;
    // }

    public enum FeatureType {
        TEXT_DETECTION
    }

    private string newUrl = "https://firestore.googleapis.com/v1/projects/arfoodappocr/databases/(default)/documents/restaurants/";

    [System.Serializable]
    public class Latitude {
        public double doubleValue;
    }

    [System.Serializable]
    public class RestaurantName {
        public string stringValue;
    }

    [System.Serializable]
    public class Path {
        public string stringValue;
    }

    [System.Serializable]
    public class Dish {
        public string stringValue;
    }

    [System.Serializable]
    public class Fields2 {
        public Path path;
        public Dish dish;
    }

    [System.Serializable]
    public class MapValue {
        public Fields2 fields;
    }

    [System.Serializable]
    public class Value {
        public MapValue mapValue;
    }

    [System.Serializable]
    public class ArrayValue {
        public List<Value> values;
    }

    [System.Serializable]
    public class Dishes {
        public ArrayValue arrayValue;
    }

    [System.Serializable]
    public class Longitude {
        public double doubleValue;
    }

    [System.Serializable]
    public class Fields {
        public Latitude latitude;
        public RestaurantName restaurantName;
        public Dishes dishes;
        public Longitude longitude;
    }

    [System.Serializable]
    public class Document {
        public string name;
        public Fields fields;
        public DateTime createTime;
        public DateTime updateTime;
    }

    [System.Serializable]
    public class RootObject {
        public List<Document> documents;
    }

    public RootObject restaurantResponses;

    // Start is called before the first frame update
    void Start () {
        //dishes = new List<string> ();

        // dishes.Add ("Spaghetti Bolognese");
        // dishes.Add ("Biefstuk met frieten");
        // dishes.Add ("Lasagne");
        // dishes.Add ("Een gerecht met een lange naam");

        headers = new Dictionary<string, string> ();
        headers.Add ("Content-Type", "application/json; charset=UTF-8");

        StartCoroutine ("FetchRestaurantData");

        if (apiKey == null || apiKey == "")
            Debug.LogError ("No API key. Please set your API key into the \"Web Cam Texture To Cloud Vision(Script)\" component.");

        defaultBackground = background.texture;
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0) {
            Debug.Log ("no camera detected");
            camAvailable = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++) {
            if (!devices[i].isFrontFacing) {
                backCam = new WebCamTexture (devices[i].name, Screen.width, Screen.height);
            }
        }

        if (backCam == null) {
            Debug.Log ("Unable to find camera");
            return;
        }

        backCam.Play ();
        background.texture = backCam;

        camAvailable = true;

        Button btn = myButton.GetComponent<Button> ();
        btn.onClick.AddListener (handleClickButton);

    }

    void handleClickButton () {
        Debug.Log ("Button click");
        StartCoroutine ("Capture");

    }

    private IEnumerator Capture () {
        //while (true) {
        if (this.apiKey == null)
            yield return null;

        //yield return new WaitForSeconds (captureIntervalSeconds);

        Color[] pixels = backCam.GetPixels ();
        if (pixels.Length == 0)
            yield return null;
        if (texture2D == null || backCam.width != texture2D.width || backCam.height != texture2D.height) {
            texture2D = new Texture2D (backCam.width, backCam.height, TextureFormat.RGBA32, false);
        }

        texture2D.SetPixels (pixels);
        // texture2D.Apply(false); // Not required. Because we do not need to be uploaded it to GPU
        byte[] jpg = texture2D.EncodeToJPG ();
        string base64 = System.Convert.ToBase64String (jpg);

        AnnotateImageRequests requests = new AnnotateImageRequests ();
        requests.requests = new List<AnnotateImageRequest> ();

        AnnotateImageRequest request = new AnnotateImageRequest ();
        request.image = new Image ();
        request.image.content = base64;
        request.features = new List<Feature> ();

        Feature feature = new Feature ();
        feature.type = this.featureType.ToString ();
        feature.maxResults = this.maxResults;

        request.features.Add (feature);

        requests.requests.Add (request);

        string jsonData = JsonUtility.ToJson (requests, false);
        if (jsonData != string.Empty) {
            string url = this.url + this.apiKey;
            byte[] postData = System.Text.Encoding.Default.GetBytes (jsonData);
            using (WWW www = new WWW (url, postData, headers)) {
                yield return www;
                if (string.IsNullOrEmpty (www.error)) {

                    Debug.Log ("www text" + www.text);

                    AnnotateImageResponses responses = JsonUtility.FromJson<AnnotateImageResponses> (www.text);
                    // SendMessage, BroadcastMessage or someting like that.
                    Debug.Log ("eerste log na annotate" + responses);
                    Sample_OnAnnotateImageResponses (responses);
                } else {
                    Debug.Log ("Error: " + www.error);
                }
            }
        }
        //}
    }

    void Sample_OnAnnotateImageResponses (AnnotateImageResponses responses) {
        Debug.Log ("eerste log IN annotate");
        if (responses.responses.Count > 0) {

            if (responses.responses[0].textAnnotations != null && responses.responses[0].textAnnotations.Count > 0) {
                //Debug.Log ("er zijn textannos" + responses.responses[0].textAnnotations[0].description);
                string detectedText = responses.responses[0].textAnnotations[0].description;
                //Debug.Log ("detected" + detectedText);

                string[] detectedTextArray = detectedText.Split (
                    new [] { "\r\n", "\r", "\n" },
                    System.StringSplitOptions.None
                );

                matchedDishes = new List<string> ();

                foreach (string s in detectedTextArray) {

                    foreach (string detectedString in dishes) {

                        if (detectedString.ToLower () == s.ToLower ()) {
                            Debug.Log ("Er is een match: " + s);
                            matchedDishes.Add (s);

                        }
                        // Debug.Log ("detected string" + detectedString.ToLower ());
                        // Debug.Log ("dishes array" + s.ToLower ());
                    }

                }

                Debug.Log (matchedDishes);

                foreach (string matchedText in matchedDishes) {
                    Debug.Log (matchedText);
                    //myText.text = $"{myText.text} \n {matchedText}";
                }

                SceneManager.LoadScene ("ARDishSelectionMenu");

                //var index = Array.FindIndex(stringArray, x => x == value)
            }

        }
    }

    private IEnumerator FetchRestaurantData () {

        // Function to fetch json data from firestore database

        using (UnityWebRequest webRequest = UnityWebRequest.Get (newUrl)) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest ();

            string[] pages = newUrl.Split ('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError) {
                // Throw an error if there is an error
                Debug.Log (pages[page] + ": Error: " + webRequest.error);
            } else {

                Debug.Log (webRequest.downloadHandler.text);

                // Deserialize the JSON data
                string jsonString = webRequest.downloadHandler.text;
                restaurantResponses = JsonUtility.FromJson<RootObject> (jsonString);

                handleFetchResponse ();

                //Debug.Log (responses.fields.dishes.arrayValue.values[0].mapValue.fields.dish.stringValue);

                // foreach (var restaurant in responses.) {
                //     Debug.Log (restaurant.name);

                //     // double restaurantLatitude = restaurant.fields.latitude.doubleValue;
                //     // double restaurantLongitude = restaurant.fields.longitude.doubleValue;

                //     // Debug.Log (restaurantLatitude);
                //     // Debug.Log (restaurantLongitude);

                //     // if (locationInfo.latitude <= restaurantLatitude + 0.0005 && locationInfo.latitude >= restaurantLatitude - 0.0005 && locationInfo.longitude <= restaurantLongitude + 0.0005 && locationInfo.longitude >= restaurantLongitude - 0.0005) {
                //     //     Debug.Log ("juiste locatie");
                //     //     currentLocation = restaurant.fields.restaurantName.stringValue;

                //     //     // load the next scene
                //     //     SceneManager.LoadScene ("OCROnboarding");
                //     //     return;

                //     // } else {
                //     //     Debug.Log ("niet juiste locatie");
                //     // }

                // }
            }
        }

    }

    public void handleFetchResponse () {
        //ebug.Log (restaurantResponses.name);

        dishes = new List<string> ();

        // Fields fields = new Fields ();
        // fields = restaurantResponses.fields;
        // Latitude restoLatitude = new Latitude ();
        // restoLatitude = fields.latitude;
        // Debug.Log (restoLatitude);

        foreach (var restaurant in restaurantResponses.documents) {

            if (restaurant.name == "projects/arfoodappocr/databases/(default)/documents/restaurants/0O4UM6nOfW7lErHT0feJ") {
                Debug.Log ("deze log zie ik maar1 keer");

                foreach (var dish in restaurant.fields.dishes.arrayValue.values) {
                    Debug.Log (dish.mapValue.fields.dish.stringValue);
                    dishes.Add (dish.mapValue.fields.dish.stringValue);

                }

            }

            // double restaurantLatitude = restaurant.fields.latitude.doubleValue;
            // double restaurantLongitude = restaurant.fields.longitude.doubleValue;

            // Debug.Log (restaurantLatitude);
            // Debug.Log (restaurantLongitude);

            // if (locationInfo.latitude <= restaurantLatitude + 0.0005 && locationInfo.latitude >= restaurantLatitude - 0.0005 && locationInfo.longitude <= restaurantLongitude + 0.0005 && locationInfo.longitude >= restaurantLongitude - 0.0005) {
            //     Debug.Log ("juiste locatie");
            //     currentLocation = restaurant.fields.restaurantName.stringValue;

            //     // load the next scene
            //     SceneManager.LoadScene ("OCROnboarding");
            //     return;

            // } else {
            //     Debug.Log ("niet juiste locatie");
            // }

        }

    }

    // Update is called once per frame
    void Update () {

        if (!camAvailable) {
            return;
        }

        float ratio = (float) backCam.width / (float) backCam.height;
        fit.aspectRatio = ratio;

        float scaleY = backCam.videoVerticallyMirrored ? -1f : 1f;
        background.rectTransform.localScale = new Vector3 (1f, scaleY, 1f);

        int orient = -backCam.videoRotationAngle;
        background.rectTransform.localEulerAngles = new Vector3 (0, 0, orient);
    }
}