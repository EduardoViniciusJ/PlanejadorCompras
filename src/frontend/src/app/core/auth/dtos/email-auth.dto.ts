export interface EmailPasswordRequestDto {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequestDto extends EmailPasswordRequestDto {
  readonly name?: string;
  readonly acceptedTerms: boolean;
}

export interface ForgotPasswordRequestDto {
  readonly email: string;
}

export interface TokenRequestDto {
  readonly token: string;
}

export interface ResetPasswordRequestDto extends TokenRequestDto {
  readonly newPassword: string;
}

export interface AuthMessageResponseDto {
  readonly message: string;
}
