using OpenCvSharp;

namespace Api.Services
{
    public interface IFaceAnonymizationService
    {
        Task<byte[]> AnonymizeFacesAsync(byte[] imageBytes);
    }

    public class FaceAnonymizationService : IFaceAnonymizationService
    {
        private readonly List<CascadeClassifier> _faceCascades;
        private readonly string[] _cascadeFiles = new[]
        {
            "haarcascade_frontalface_alt.xml",      // Фронтальні обличчя (альтернативний)
            "haarcascade_frontalface_default.xml",  // Фронтальні обличчя (стандартний)
            "haarcascade_frontalface_alt2.xml",     // Фронтальні обличчя (варіант 2)
            "haarcascade_profileface.xml"           // Профільні обличчя
        };

        public FaceAnonymizationService()
        {
            _faceCascades = new List<CascadeClassifier>();
            
            // Завантажуємо всі доступні каскади
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

        public async Task<byte[]> AnonymizeFacesAsync(byte[] imageBytes)
        {
            return await Task.Run(() =>
            {
                return AnonymizeFaces(imageBytes);
            });
        }

        public byte[] AnonymizeFaces(byte[] imageBytes)
        {
            try
            {
                // Завантажуємо зображення з байтів
                using var mat = Mat.FromImageData(imageBytes);

                // Конвертуємо в сірий колір для детекції
                using var grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);

                // Детектуємо обличчя
                var faces = DetectFaces(grayMat);

                // Анонімізуємо кожне обличчя
                foreach (var face in faces)
                {
                    AnonymizeFace(mat, face);
                }

                // Конвертуємо назад в байти
                return mat.ToBytes(".jpg");
            }
            catch (Exception ex)
            {
                // Логування помилки
                Console.WriteLine($"Error in face anonymization: {ex.Message}");
                // Повертаємо оригінальне зображення у випадку помилки
                return imageBytes;
            }
        }

        private Rect[] DetectFaces(Mat grayImage)
        {
            var allFaces = new List<Rect>();
            
            // Якщо каскади не завантажені, використовуємо тестову область
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
                // Прогоняємо зображення через всі завантажені каскади
                foreach (var cascade in _faceCascades)
                {
                    if (cascade.Empty()) continue;
                    
                    try
                    {
                        var faces = cascade.DetectMultiScale(
                            grayImage,
                            scaleFactor: 1.1,           // Коефіцієнт масштабування
                            minNeighbors: 3,            // Мінімальна кількість сусідів
                            flags: HaarDetectionTypes.ScaleImage,
                            minSize: new Size(10, 10),  // Мінімальний розмір обличчя
                            maxSize: new Size(700, 700) // Максимальний розмір обличчя
                        );
                        
                        allFaces.AddRange(faces);
                        Console.WriteLine($"Cascade detected {faces.Length} faces");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in cascade detection: {ex.Message}");
                    }
                }

                // Видаляємо дублікати та обличчя, що перекриваються
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
                    // Перевіряємо перекриття (якщо більше 50% площі перекривається)
                    var intersection = face & existingFace; // Оператор перетину
                    var unionArea = face.Width * face.Height + existingFace.Width * existingFace.Height 
                                  - intersection.Width * intersection.Height;
                    
                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        double overlapRatio = (double)(intersection.Width * intersection.Height) / 
                                            Math.Min(face.Width * face.Height, existingFace.Width * existingFace.Height);
                        
                        if (overlapRatio > 0.5) // 50% перекриття
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

        private void AnonymizeFace(Mat image, Rect faceRect)
        {
            // Метод 1: Розмиття обличчя
            BlurFace(image, faceRect);
            
            // Метод 2: Піксельація (коментар, щоб використати тільки один)
            // PixelateFace(image, faceRect);
            
            // Метод 3: Чорний прямокутник (коментар)
            // BlackoutFace(image, faceRect);
        }

        private void BlurFace(Mat image, Rect faceRect)
        {
            try
            {
                // Вирізаємо область обличчя
                using var faceRegion = new Mat(image, faceRect);
                
                // Застосовуємо сильне розмиття
                using var blurredFace = new Mat();
                Cv2.GaussianBlur(faceRegion, blurredFace, new Size(99, 99), 0);
                
                // Копіюємо розмите обличчя назад
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
                // Вирізаємо область обличчя
                using var faceRegion = new Mat(image, faceRect);
                
                // Зменшуємо розмір для піксельації
                using var smallFace = new Mat();
                Cv2.Resize(faceRegion, smallFace, new Size(20, 20), interpolation: InterpolationFlags.Nearest);
                
                // Збільшуємо назад до оригінального розміру
                using var pixelatedFace = new Mat();
                Cv2.Resize(smallFace, pixelatedFace, faceRect.Size, interpolation: InterpolationFlags.Nearest);
                
                // Копіюємо піксельоване обличчя назад
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
                // Заповнюємо область чорним кольором
                Cv2.Rectangle(image, faceRect, Scalar.Black, -1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error blacking out face: {ex.Message}");
            }
        }

        public void Dispose()
        {
            foreach (var cascade in _faceCascades)
            {
                cascade?.Dispose();
            }
            _faceCascades.Clear();
        }
    }
}