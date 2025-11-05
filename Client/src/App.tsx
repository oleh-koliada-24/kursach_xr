import { useState } from "react";
import { Button, Card, Form } from "react-bootstrap";
import axios from "axios";

const url = 'http://localhost:5245/api/';
const noImagePlaceholder = "src/assets/no-image-placeholder.png";

function App() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [anonymizedPictureSrc, setAnonymizedPictureSrc] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const anonymizePicture = async () => {
    if (!selectedFile) {
      alert("Please select a picture first.");
      return;
    }

    setIsLoading(true);
    
    try {
      const formData = new FormData();
      formData.append('image', selectedFile);

      const response = await axios.post(`${url}anonymization`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        responseType: 'blob'
      });

      const anonymizedImageUrl = URL.createObjectURL(response.data);
      setAnonymizedPictureSrc(anonymizedImageUrl);
      
    } catch (error) {
      console.error('Anonymization error:', error);
      alert('Error occurred while anonymizing the picture. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="d-flex flex-row gap-3 align-items-center">
      <Card className="p-3 d-flex flex-column align-items-center">
        <Card.Img 
          variant="top" 
          src={selectedFile ? URL.createObjectURL(selectedFile) : noImagePlaceholder} 
          style={{ width: '400px', height: '289px', objectFit: 'cover' }}
        />
          <Form.Label htmlFor="file-input" className="btn btn-primary w-100 mt-3 mb-0">
            Select picture to anonymize
          </Form.Label>
          <Form.Control
            id="file-input"
            type="file" 
            accept="image/*"
            style={{ display: 'none' }}
            onChange={e => {
              const file = (e.target as HTMLInputElement).files?.[0];
              if (file) {
                setSelectedFile(file);
                setAnonymizedPictureSrc("");
              }
            }}
          />
      </Card>

      <div className="d-flex align-items-center">→</div>

      <Card className="p-3 d-flex flex-column align-items-center">
        <Card.Img 
          variant="top" 
          src={anonymizedPictureSrc || noImagePlaceholder}
          style={{ width: '400px', height: '289px', objectFit: 'cover' }}
        />
        <Button
          variant="primary"
          className="mt-3 w-100"
          onClick={anonymizePicture}
          disabled={!selectedFile || isLoading}
        >
          {isLoading ? (
            <>
              <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              Anonymizing...
            </>
          ) : (
            'Anonymize Picture'
          )}
        </Button>
      </Card>
    </div>
  )
}

export default App
