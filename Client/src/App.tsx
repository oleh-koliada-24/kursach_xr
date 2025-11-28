import { useState, useEffect, useMemo } from "react";
import { Button, Card, Form } from "react-bootstrap";
import axios from "axios";
import * as signalR from "@microsoft/signalr";

const url = 'http://localhost:5245/api/';
const noImagePlaceholder = "src/assets/no-image-placeholder.png";

const AnonymizationType = {
  Blur: 0,
  Pixelate: 1,
  Blackout: 2
} as const;

type AnonymizationTypeValue = typeof AnonymizationType[keyof typeof AnonymizationType];

function App() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [anonymizedPictureSrc, setAnonymizedPictureSrc] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isAnonymizationFinishedSuccessful, setIsAnonymizationFinishedSuccessful] = useState<boolean>(false);
  const [anonymizationType, setAnonymizationType] = useState<AnonymizationTypeValue>(AnonymizationType.Blur);
  const [progress, setProgress] = useState<number>(0);
  const [sessionId] = useState<string>(() => crypto.randomUUID());
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [anonymizedBlob, setAnonymizedBlob] = useState<Blob | null>(null);
  const [fileExtension, setFileExtension] = useState<string>('jpg');

  const selectedFileUrl = useMemo(() => {
    if (!selectedFile) return noImagePlaceholder;
    return URL.createObjectURL(selectedFile);
  }, [selectedFile]);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5245/anonymizationHub")
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then(() => {
          console.log("SignalR Connected!");

          connection.on("ReceiveProgress", (sid: string, current: number, total: number, percentage: number) => {
            if (sid === sessionId) {
              setProgress(percentage);
              console.log(`Progress: ${current}/${total} (${percentage}%)`);
            }
          });
        })
        .catch((err) => console.error("SignalR Connection Error: ", err));
    }

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, [connection, sessionId]);

  const downloadAnonymizedImage = () => {
    if (!anonymizedBlob) return;

    const url = URL.createObjectURL(anonymizedBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `anonymized_${Date.now()}.${fileExtension}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const anonymizePicture = async () => {
    if (!selectedFile) {
      alert("Please select a picture first.");
      return;
    }

    setIsLoading(true);
    setProgress(0);
    
    try {
      const formData = new FormData();
      formData.append('image', selectedFile);
      formData.append('type', anonymizationType.toString());
      formData.append('sessionId', sessionId);

      const fileExt = selectedFile.name.split('.').pop() || 'jpg';
      setFileExtension(fileExt);

      const response = await axios.post(`${url}anonymization`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        responseType: 'blob'
      });

      setAnonymizedBlob(response.data);
      const anonymizedImageUrl = URL.createObjectURL(response.data);
      setAnonymizedPictureSrc(anonymizedImageUrl);
      setIsAnonymizationFinishedSuccessful(true);
    } catch (error) {
      console.error('Anonymization error:', error);
      alert('Error occurred while anonymizing the picture. Please try again.');
    } finally {
      setIsLoading(false);
      setProgress(0);
    }
  };

  return (
    <div className="d-flex flex-row gap-3 align-items-center">
      <Card className="p-3 d-flex flex-column align-items-center">
        <Card.Img 
          variant="top" 
          src={selectedFileUrl} 
          style={{ width: '400px', height: '289px', objectFit: 'contain' }}
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
                setAnonymizedBlob(null);
                setIsAnonymizationFinishedSuccessful(false);
              }
            }}
          />
      </Card>

      <div className="d-flex flex-column align-items-center gap-3">
        <div className="fs-2">→</div>
        <div className="text-center">
          <Form.Label className="fw-bold mb-2">Select Anonymization Method</Form.Label>
          <Form.Select 
            value={anonymizationType} 
            onChange={(e) => {
              setAnonymizationType(parseInt(e.target.value) as AnonymizationTypeValue);
              setAnonymizedPictureSrc("");
              setAnonymizedBlob(null);
              setIsAnonymizationFinishedSuccessful(false);
            }}
          >
            <option value={AnonymizationType.Blur}>Blur</option>
            <option value={AnonymizationType.Pixelate}>Pixelate</option>
            <option value={AnonymizationType.Blackout}>Blackout</option>
          </Form.Select>
        </div>
      </div>

      <Card className="p-3 d-flex flex-column align-items-center">
        <Card.Img 
          variant="top" 
          src={anonymizedPictureSrc || noImagePlaceholder}
          style={{ width: '400px', height: '289px', objectFit: 'contain' }}
        />
        {isAnonymizationFinishedSuccessful
          ? <Button
            variant="success"
            className="mt-2 w-100"
            onClick={downloadAnonymizedImage}
            disabled={!anonymizedBlob}
          >
            Download Anonymized Image
          </Button>

          : <Button
            variant="primary"
            className="mt-3 w-100"
            onClick={anonymizePicture}
            disabled={!selectedFile || isLoading}
          >
            {isLoading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                {progress > 0 ? `Anonymizing (${progress}%)...` : 'Anonymizing...'}
              </>
            ) : (
              `Anonymize with ${Object.keys(AnonymizationType)[Object.values(AnonymizationType).indexOf(anonymizationType)]}`
            )}
          </Button>
        }
      </Card>
    </div>
  )
}

export default App
