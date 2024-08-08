export class ChangePasswordRequest {
    password: string;

    constructor(newPass: string) {
        this.password = newPass;
    }
}