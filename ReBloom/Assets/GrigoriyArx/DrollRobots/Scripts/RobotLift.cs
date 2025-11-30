using UnityEngine;

public class RoboLift : MonoBehaviour
{
    public CharacterController controller; // Ссылка на Character Controller
    public float liftSpeed = 2f;           // Скорость подъема
    public float maxHeight = 10f;          // Максимальная высота подъема
    public float minHeight = 0f;           // Минимальная высота
    private Vector3 moveDirection;         // Направление движения
    private float startY;                  // Начальная позиция по Y

    void Start()
    {
        // Сохраняем начальную высоту
        startY = transform.position.y;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            // Двигаемся вверх, пока не достигнута максимальная высота
            if (transform.position.y < startY + maxHeight)
            {
                moveDirection.y = liftSpeed;
            }
            else
            {
                moveDirection.y = 0;
            }
        }
        else if (Input.GetKey(KeyCode.F))
        {
            // Двигаемся вниз, пока не достигнута минимальная высота
            if (transform.position.y > startY + minHeight)
            {
                moveDirection.y = -liftSpeed;
            }
            else
            {
                moveDirection.y = 0;
            }
        }
        else
        {
            // Если клавиши не нажаты, останавливаем движение
            moveDirection.y = 0;
        }

        // Применяем движение через Character Controller
        controller.Move(moveDirection * Time.deltaTime);
    }
}
