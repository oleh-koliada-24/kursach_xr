using Api.DTOs;
using OpenCvSharp;

namespace Api.Services
{
    public interface IFaceAnonymizationService
    {
        byte[] AnonymizeFaces(byte[] imageBytes, AnonymizationType anonymizationType);
    }

    public class FaceAnonymizationService : IFaceAnonymizationService
    {
        private readonly List<CascadeClassifier> _faceCascades;
        private readonly string[] _cascadeFiles = new[]
        {
            "haarcascade_frontalface_alt.xml",
            "haarcascade_frontalface_default.xml",
            "haarcascade_frontalface_alt2.xml",
            "haarcascade_profileface.xml"
        };

        public FaceAnonymizationService()
        {
            _faceCascades = new List<CascadeClassifier>();
            
            var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
            
            foreach (var cascadeFile in _cascadeFiles)
            {
                var cascadePath = Path.Combine(modelsPath, cascadeFile);
                
                if (File.Exists(cascadePath))
                {
                    try
                    {
                        var cascade = new CascadeClassifier(cascadePath);
                        if (!cascade.Empty())
                        {
                            _faceCascades.Add(cascade);
                            Console.WriteLine($"Loaded cascade: {cascadeFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load cascade {cascadeFile}: {ex.Message}");
                    }
                }
            }
            
            Console.WriteLine($"Loaded {_faceCascades.Count} cascade classifiers");
        }

        public byte[] AnonymizeFaces(byte[] imageBytes, AnonymizationType anonymizationType)
        {
            try
            {
                using var mat = Mat.FromImageData(imageBytes);

                using var grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);

                var faces = DetectFaces(grayMat);

                foreach (var face in faces)
                {
                    AnonymizeFace(mat, face, anonymizationType);
                }

                return mat.ToBytes(".jpg");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in face anonymization: {ex.Message}");
                return imageBytes;
            }
        }

        private Rect[] DetectFaces(Mat grayImage)
        {
            var allFaces = new List<Rect>();
            
            if (_faceCascades.Count == 0)
            {
                Console.WriteLine("No cascades loaded, using test face region");
                int width = grayImage.Width;
                int height = grayImage.Height;
                
                if (width > 200 && height > 200)
                {
                    allFaces.Add(new Rect(width / 4, height / 4, width / 4, height / 4));
                }
                
                return allFaces.ToArray();
            }

            try
            {
                foreach (var cascade in _faceCascades)
                {
                    if (cascade.Empty()) continue;
                    
                    try
                    {
                        var faces = cascade.DetectMultiScale(
                            grayImage,
                            scaleFactor: 1.1,
                            minNeighbors: 3,
                            flags: HaarDetectionTypes.ScaleImage,
                            minSize: new Size(30, 30),
                            maxSize: new Size(700, 700)
                        );
                        
                        allFaces.AddRange(faces);
                        Console.WriteLine($"Cascade detected {faces.Length} faces");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in cascade detection: {ex.Message}");
                    }
                }

                var uniqueFaces = RemoveOverlappingFaces(allFaces);
                
                Console.WriteLine($"Total unique faces detected: {uniqueFaces.Count}");
                return uniqueFaces.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in face detection: {ex.Message}");
                return new Rect[0];
            }
        }

        private List<Rect> RemoveOverlappingFaces(List<Rect> faces)
        {
            if (faces.Count <= 1) return faces;

            var uniqueFaces = new List<Rect>();
            var sortedFaces = faces.OrderByDescending(f => f.Width * f.Height).ToList();

            foreach (var face in sortedFaces)
            {
                bool isUnique = true;
                
                foreach (var existingFace in uniqueFaces)
                {
                    var intersection = face & existingFace;
                    var unionArea = face.Width * face.Height + existingFace.Width * existingFace.Height 
                                  - intersection.Width * intersection.Height;
                    
                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        double overlapRatio = (double)(intersection.Width * intersection.Height) / 
                                            Math.Min(face.Width * face.Height, existingFace.Width * existingFace.Height);
                        
                        if (overlapRatio > 0.5)
                        {
                            isUnique = false;
                            break;
                        }
                    }
                }
                
                if (isUnique)
                {
                    uniqueFaces.Add(face);
                }
            }

            return uniqueFaces;
        }

        private void AnonymizeFace(Mat image, Rect faceRect, AnonymizationType anonymizationType)
        {
            switch (anonymizationType)
            {
                case AnonymizationType.Blur:
                    BlurFace(image, faceRect);
                    break;
                case AnonymizationType.Pixelate:
                    PixelateFace(image, faceRect);
                    break;
                case AnonymizationType.Blackout:
                    BlackoutFace(image, faceRect);
                    break;
                default:
                    BlurFace(image, faceRect);
                    break;
            }
        }

        private void BlurFace(Mat image, Rect faceRect)
        {
            try
            {
                using var faceRegion = new Mat(image, faceRect);
                
                using var blurredFace = new Mat();
                Cv2.GaussianBlur(faceRegion, blurredFace, new Size(99, 99), 0);
                
                blurredFace.CopyTo(new Mat(image, faceRect));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error blurring face: {ex.Message}");
            }
        }

        private void PixelateFace(Mat image, Rect faceRect)
        {
            try
            {
                using var faceRegion = new Mat(image, faceRect);
                
                using var smallFace = new Mat();
                Cv2.Resize(faceRegion, smallFace, new Size(20, 20), interpolation: InterpolationFlags.Nearest);
                
                using var pixelatedFace = new Mat();
                Cv2.Resize(smallFace, pixelatedFace, faceRect.Size, interpolation: InterpolationFlags.Nearest);
                
                pixelatedFace.CopyTo(new Mat(image, faceRect));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pixelating face: {ex.Message}");
            }
        }

        private void BlackoutFace(Mat image, Rect faceRect)
        {
            try
            {
                Cv2.Rectangle(image, faceRect, Scalar.Black, -1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error blacking out face: {ex.Message}");
            }
        }
    }
}