using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pigeon : MonoBehaviour
{
    private Animator pigeon;
    public float gravity = 1.0f;
    private Vector3 moveDirection = Vector3.zero;
    CharacterController characterController;

    void Start()
    {
        pigeon = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }
    void Update()
    {
        characterController.Move(moveDirection * Time.deltaTime);
        moveDirection.y = gravity * Time.deltaTime;
        if (pigeon.GetCurrentAnimatorStateInfo(0).IsName("fly"))
        {
            pigeon.SetBool("takeoff", false);
            pigeon.SetBool("landing", false);
            pigeon.SetBool("glide", false);
        }
        if (pigeon.GetCurrentAnimatorStateInfo(0).IsName("flyleft"))
        {
            pigeon.SetBool("takeoff", false);
            pigeon.SetBool("landing", false);
            pigeon.SetBool("glide", false);
        }
        if (pigeon.GetCurrentAnimatorStateInfo(0).IsName("flyright"))
        {
            pigeon.SetBool("takeoff", false);
            pigeon.SetBool("landing", false);
            pigeon.SetBool("glide", false);
        }
        if (pigeon.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            pigeon.SetBool("eat", false);
            pigeon.SetBool("preen", false);
            pigeon.SetBool("scratching", false);
            pigeon.SetBool("landing", false);
            pigeon.SetBool("walk", false);
            pigeon.SetBool("walkleft", false);
            pigeon.SetBool("walkright", false);
            pigeon.SetBool("glide", false);
            pigeon.SetBool("takeoff", false);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            pigeon.SetBool("takeoff", true);
            pigeon.SetBool("landing", true);
            pigeon.SetBool("idle", false);
            pigeon.SetBool("fly", false);
            pigeon.SetBool("flyleft", false);
            pigeon.SetBool("flyright", false);
            pigeon.SetBool("glide", false);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            pigeon.SetBool("walk", true);
            pigeon.SetBool("idle", false);
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            pigeon.SetBool("walk", false);
            pigeon.SetBool("idle", true);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            pigeon.SetBool("flyleft", true);
            pigeon.SetBool("fly", false);
            pigeon.SetBool("walkleft", true);
            pigeon.SetBool("walk", false);
            pigeon.SetBool("idle", false);
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            pigeon.SetBool("fly", true);
            pigeon.SetBool("flyleft", false);
            pigeon.SetBool("walk", true);
            pigeon.SetBool("walkleft", false);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            pigeon.SetBool("flyright", true);
            pigeon.SetBool("fly", false);
            pigeon.SetBool("walkright", true);
            pigeon.SetBool("walk", false);
            pigeon.SetBool("idle", false);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            pigeon.SetBool("fly", true);
            pigeon.SetBool("flyright", false);
            pigeon.SetBool("walk", true);
            pigeon.SetBool("walkright", false);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            pigeon.SetBool("eat", true);
            pigeon.SetBool("idle", false);
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            pigeon.SetBool("preen", true);
            pigeon.SetBool("idle", false);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            pigeon.SetBool("scratching", true);
            pigeon.SetBool("idle", false);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            pigeon.SetBool("glide", true);
            pigeon.SetBool("fly", false);
            pigeon.SetBool("flyleft", false);
            pigeon.SetBool("flyright", false);
        }
    }
}
